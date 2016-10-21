using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using GraphQL.Net.SchemaAdapters;
using GraphQL.Parser;
using GraphQL.Parser.CS;
using GraphQL.Parser.Execution;
using Microsoft.FSharp.Core;

namespace GraphQL.Net
{
    internal static class Executor<TContext>
    {
        public static object Execute
            (GraphQLSchema<TContext> schema, GraphQLField field, ExecSelection<Info> query)
        {
            var context = schema.ContextCreator();
            var results = Execute(schema, context, field, query);
            (context as IDisposable)?.Dispose();
            return results;
        }

        public static object Execute
            (GraphQLSchema<TContext> schema, TContext context, GraphQLField field, ExecSelection<Info> query)
        {
            var mutReturn = field.RunMutation(context, query.Arguments.Values());

            var queryableFuncExpr = field.GetExpression(query.Arguments.Values(), mutReturn);
            var replaced = (LambdaExpression)ParameterReplacer.Replace(queryableFuncExpr, queryableFuncExpr.Parameters[0], GraphQLSchema<TContext>.DbParam);

            // sniff queryable provider to determine how selector should be built
            var dummyQuery = replaced.Compile().DynamicInvoke(context, null);
            var queryType = dummyQuery.GetType();
            var queryExecSelections = query.Selections.Values();
            var selector = GetSelector(schema, field.Type, queryExecSelections, schema.GetOptionsForQueryable(queryType));

            if (field.ResolutionType != ResolutionType.Unmodified)
            {
                var selectorExpr = Expression.Quote(selector);

                // TODO: This should be temporary - queryable and enumerable should both work 
                var body = replaced.Body;
                if (body.NodeType == ExpressionType.Convert)
                    body = ((UnaryExpression)body).Operand;

                var call = Expression.Call(typeof(Queryable), "Select", new[] { field.Type.CLRType, field.Type.QueryType }, body, selectorExpr);
                var expr = Expression.Lambda(call, GraphQLSchema<TContext>.DbParam);
                var transformed = expr.Compile().DynamicInvoke(context);

                object results;
                switch (field.ResolutionType)
                {
                    case ResolutionType.Unmodified:
                        throw new Exception("Queries cannot have unmodified resolution. May change in the future.");
                    case ResolutionType.ToList:
                        var list = GenericQueryableCall<IEnumerable<object>>(transformed, q => q.ToList());
                        results = list.Select(o => MapResults(o, queryExecSelections, schema)).ToList();
                        break;
                    case ResolutionType.FirstOrDefault:
                        var fod = GenericQueryableCall(transformed, q => q.FirstOrDefault());
                        results = MapResults(fod, queryExecSelections, schema);
                        break;
                    case ResolutionType.First:
                        var first = GenericQueryableCall(transformed, q => q.FirstOrDefault());
                        results = MapResults(first, queryExecSelections, schema);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                return results;
            }
            else
            {
                var invocation = Expression.Invoke(selector, replaced.Body);
                var expr = Expression.Lambda(invocation, GraphQLSchema<TContext>.DbParam);
                var result = expr.Compile().DynamicInvoke(context);
                return MapResults(result, queryExecSelections, schema);
            }
        }

        // This takes a Queryable<T> as an object, and executes a T-typed method call against it
        // That is, you define a `call` in terms of Queryable<object> and it will be converted to Queryable<T>
        //
        // ex:  GenericQueryableCall(queryableAsObj, q => q.FirstOrDefault());
        //
        // In this case, the generic method is represented at compiled time as FirstOrDefault<object>.
        // However, the generic type is replaced at runtime to FirstOrDefault<T> then dynamically invoked on `queryable`
        //
        // This is necessary for NHibernate, which inspects the generic type of the method executing the queryable.
        // So, if the queryable is of compile-time type IQueryable<object> instead of IQueryable<TEntity>, NHibernate will fail on execution
        private static TReturn GenericQueryableCall<TReturn>(object queryable, Expression<Func<IQueryable<object>, TReturn>> call)
        {
            var entityType = queryable.GetType().GetGenericArguments()[0];
            var mce = call.Body as MethodCallExpression;
            var genericMethod = mce.Method.GetGenericMethodDefinition();

            var newMethod = genericMethod.MakeGenericMethod(entityType);
            var newParameter = Expression.Parameter(typeof(IQueryable<>).MakeGenericType(entityType));
            var newArguments = mce.Arguments.Select(a => ParameterReplacer.Replace(a, call.Parameters[0], newParameter));
            var newCall = Expression.Call(newMethod, newArguments);
            var newLambda = Expression.Lambda(newCall, newParameter);

            return (TReturn)newLambda.Compile().DynamicInvoke(queryable);
        }

        private static IDictionary<string, object> MapResults(object queryObject, IEnumerable<ExecSelection<Info>> selections, GraphQLSchema<TContext> schema)
        {
            if (queryObject == null) // TODO: Check type non-null and throw exception
                return null;
            var dict = new Dictionary<string, object>();
            var type = queryObject.GetType();
            foreach (var map in selections)
            {
                var key = map.Name;
                var field = map.SchemaField.Field();
                var obj = field.IsPost
                    ? field.PostFieldFunc()
                    : type.GetProperty(field.Name).GetGetMethod().Invoke(queryObject, new object[] { });


                // Filter fields for selections with type conditions - the '__typename'-property has to be present.
                if (map.TypeCondition != null)
                {
                    // The selection has a type condition, i.e. the result has a type with included types.
                    // The result contains all fields of all included types and has to be filtered.
                    // Sample:
                    // 
                    // Types: 
                    //      - Character     [name: string]
                    //      - Human         [height: float]           extends Character
                    //      - Stormtrooper  [specialization: string]  extends Human
                    //      - Droid         [orimaryFunction: string] extends Character
                    // 
                    // Sample query:
                    //      query { heros { name, ... on Human { height }, ... on Stormtrooper { specialization }, ... on Droid { primaryFunction } } }
                    // 
                    // The ExecSelection for 'height', 'specialization' and 'primaryFunction' have a type condition with the following type names:
                    //      - height:           Human
                    //      - specialization:   Stormtrooper
                    //      - primaryFunction:  Droid
                    //
                    // To filter the result properly, we have to consider the following cases:
                    // - Human: 
                    //      - Include: 'name', 'height'
                    //      - Exclude: 'specialization', 'primaryFunction'
                    //  => (1) Filter results of selections with a type-condition-name != result.__typename
                    //
                    //  - Stormtrooper
                    //      - Include: 'name', 'height', and 'specialization'
                    //      - Exclude: 'primaryFunction'
                    // => Same as Human (1), but:
                    //  => (2) include results of selections with the same type-condition-name of any ancestor-type.
                    //
                    var selectionConditionTypeName = map.TypeCondition.Value?.TypeName;
                    var typenameProp = type.GetRuntimeProperty("__typename");
                    var resultTypeName = (string)typenameProp?.GetValue(queryObject);

                    // (1) type-condition-name != result.__typename
                    if (selectionConditionTypeName != resultTypeName)
                    {
                        // (2) Check ancestor types
                        var resultGraphQlType = schema.Types.FirstOrDefault(t => t.Name == resultTypeName);
                        if (resultGraphQlType != null && !IsEqualToAnyAncestorType(resultGraphQlType, selectionConditionTypeName))
                        {
                            continue;
                        }
                    }
                }

                if (obj == null)
                {
                    dict.Add(key, null);
                    continue;
                }

                if (field.IsPost && map.Selections.Any())
                {
                    var selector = GetSelector(schema, field.Type, map.Selections.Values(), new ExpressionOptions(null, castAssignment: true));
                    obj = selector.Compile().DynamicInvoke(obj);
                }

                if (map.Selections.Any())
                {
                    var listObj = obj as IEnumerable<object>;
                    if (listObj != null)
                    {
                        dict.Add(key, listObj.Select(o => MapResults(o, map.Selections.Values(), schema)).ToList());
                    }
                    else if (obj != null)
                    {
                        dict.Add(key, MapResults(obj, map.Selections.Values(), schema));
                    }
                    else
                    {
                        throw new Exception("Shouldn't be here");
                    }
                }
                else
                {
                    dict.Add(key, obj);
                }
            }
            return dict;
        }

        private static bool IsEqualToAnyAncestorType(GraphQLType type, string referenceTypeName)
        {
            return type != null && (type.Name == referenceTypeName || IsEqualToAnyAncestorType(type?.BaseType, referenceTypeName));
        }

        private static LambdaExpression GetSelector(GraphQLSchema<TContext> schema, GraphQLType gqlType, IEnumerable<ExecSelection<Info>> selections, ExpressionOptions options)
        {
            var parameter = Expression.Parameter(gqlType.CLRType, "p");
            var init = GetMemberInit(schema, gqlType.QueryType, selections, parameter, options);
            return Expression.Lambda(init, parameter);
        }

        private static ConditionalExpression GetMemberInit(GraphQLSchema<TContext> schema, Type queryType, IEnumerable<ExecSelection<Info>> selections, Expression baseBindingExpr, ExpressionOptions options)
        {
            selections = selections.ToList();

            var bindings = selections
                .Where(m => !m.SchemaField.Field().IsPost)
                .Select(map => GetBinding(schema, map, queryType, baseBindingExpr, options))
                .ToList();

            // Add selection for '__typename'-field if not contained.
            if (selections.Any() &&
                selections.Any(s => s.TypeCondition != null) &&
                selections.All(s => s.SchemaField.FieldName != "__typename"))
            {
                var graphQlType = selections.First().SchemaField.DeclaringType.Info.Type;
                var typeNameField = graphQlType.OwnFields.Find(f => f.Name == "__typename");

                var typeNameExecSelection = new ExecSelection<Info>(
                    new SchemaField(
                        schema.Adapter.QueryTypes[graphQlType?.Name],
                        typeNameField,
                        schema.Adapter),
                    new FSharpOption<string>(typeNameField?.Name),
                    null,
                    new WithSource<ExecArgument<Info>>[] { },
                    new WithSource<ExecDirective<Info>>[] { },
                    new WithSource<ExecSelection<Info>>[] { }
                    );
                bindings = bindings.Concat(new[] { GetBinding(schema, typeNameExecSelection, queryType, baseBindingExpr, options) }).ToList();
            }

            var memberInit = Expression.MemberInit(Expression.New(queryType), bindings);
            return NullPropagate(baseBindingExpr, memberInit);
        }

        private static ConditionalExpression NullPropagate(Expression baseExpr, Expression returnExpr)
        {
            var equals = Expression.Equal(baseExpr, Expression.Constant(null));
            return Expression.Condition(equals, Expression.Constant(null, returnExpr.Type), returnExpr);
        }

        private static MemberBinding GetBinding(GraphQLSchema<TContext> schema, ExecSelection<Info> map, Type toType, Expression baseBindingExpr, ExpressionOptions options)
        {            
            var field = map.SchemaField.Field();
            var needsTypeCheck = baseBindingExpr.Type != field.DefiningType.CLRType;
            var toMember = toType.GetProperty(map.SchemaField.FieldName);
            // expr is form of: (context, entity) => entity.Field
            var expr = field.GetExpression(map.Arguments.Values());

            // The field might be defined on sub-types of the specified type, so add a type cast if necessary.
            // If appropriate, `entity.Field` becomes `(entity as TFieldDefiningType).Field`
            var typeCastedBaseExpression = needsTypeCheck ? Expression.TypeAs(baseBindingExpr, field.DefiningType.CLRType) : baseBindingExpr;
            
            // Replace (entity) with baseBindingExpr, note expression is no longer a LambdaExpression
            // `(context, entity) => entity.Field` becomes `someOtherEntity.Entity.Field` where baseBindingExpr is `someOtherEntity.Entity`
            var replacedBase = ParameterReplacer.Replace(expr.Body, expr.Parameters[1], typeCastedBaseExpression);

            // If the entity has to be casted, add a type check:
            // `(someOtherEntity.Entity as TFieldDefininingType).Field` becomes `(someOtherEntity.Entity is TFieldDefininingType) ? (someOtherEntity.Entity as TFieldDefininingType).Field : null`
            if (needsTypeCheck)
            {
                var typeCheck = Expression.TypeIs(baseBindingExpr, field.DefiningType.CLRType);
                var nullResult = Expression.Constant(null, toMember.PropertyType);
                
                // The expression type has to be nullable
                if(replacedBase.Type.IsValueType)
                {
                    replacedBase = Expression.Convert(replacedBase, typeof(Nullable<>).MakeGenericType(replacedBase.Type));
                }

                replacedBase = Expression.Condition(typeCheck, replacedBase, nullResult);
            }

            // This just makes sure that the (context) parameter is the same as the one used by the whole query
            var replacedContext = ParameterReplacer.Replace(replacedBase, expr.Parameters[0], GraphQLSchema<TContext>.DbParam);

            // If there aren't any children, then we can assume that this is a scalar entity and we don't have to map child fields
            if (!map.Selections.Any())
            {
                if (options.CastAssignment && expr.Body.Type != toMember.PropertyType)
                    replacedContext = Expression.Convert(replacedContext, toMember.PropertyType);
                return Expression.Bind(toMember, replacedContext);
            }

            // If binding a single entity, just use the already built selector expression (replaced context)
            // Otherwise, if binding to a list, introduce a new parameter that will be used in a call to .Select
            var listParameter = Expression.Parameter
                (field.Type.CLRType, field.Type.CLRType.Name.Substring(0, 1).ToLower()); // TODO: Name conflicts in expressions?
            var bindChildrenTo = map.SchemaField.Field().IsList ? listParameter : replacedContext;

            // Now that we have our new binding parameter, build the tree for the rest of the children
            var memberInit = GetMemberInit(schema, field.Type.QueryType, map.Selections.Values(), bindChildrenTo, options);

            // For single entities, we're done and we can just bind to the memberInit expression
            if (!field.IsList)
                return Expression.Bind(toMember, memberInit);

            // However for lists, we need to check for null and call .Select() and .ToList() first
            var selectLambda = Expression.Lambda(memberInit, listParameter);
            var call = Expression.Call(typeof(Enumerable), "Select", new[] { field.Type.CLRType, field.Type.QueryType }, replacedContext, selectLambda);
            var toList = Expression.Call(typeof(Enumerable), "ToList", new[] { field.Type.QueryType }, call);
            return Expression.Bind(toMember, options.NullCheckLists ? (Expression)NullPropagate(replacedContext, toList) : toList);
        }
    }
}
