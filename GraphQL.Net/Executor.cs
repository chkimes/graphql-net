using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using GraphQL.Net.SchemaAdapters;
using GraphQL.Parser.CS;
using GraphQL.Parser.Execution;

namespace GraphQL.Net
{
    internal static class Executor<TContext>
    {
        public static object Execute
            (GraphQLSchema<TContext> schema, GraphQLField field, ExecSelection<Info> query)
        {
            var context = schema.ContextCreator();
            var results = Execute(context, field, query);
            (context as IDisposable)?.Dispose();
            return results;
        }

        public static object Execute
            (TContext context, GraphQLField field, ExecSelection<Info> query)
        {
            if (field.ResolutionType != ResolutionType.Unmodified)
            {
                var queryableFuncExpr = field.GetExpression(query.Arguments.Values());
                var replaced = (LambdaExpression)ParameterReplacer.Replace(queryableFuncExpr, queryableFuncExpr.Parameters[0], GraphQLSchema<TContext>.DbParam);
                var selector = GetSelector(field.FieldCLRType, field.Type, query.Selections.Values());

                var selectorExpr = Expression.Quote(selector);
                // TODO: This should be temporary - queryable and enumerable should both work 
                var body = replaced.Body;
                if (body.NodeType == ExpressionType.Convert)
                    body = ((UnaryExpression) body).Operand;
                var call = Expression.Call(typeof(Queryable), "Select", new[] { field.FieldCLRType, field.Type.QueryType }, body, selectorExpr);
                var expr = Expression.Lambda(call, GraphQLSchema<TContext>.DbParam);
                var transformed = (IQueryable<object>)expr.Compile().DynamicInvoke(context);

                object results;
                switch (field.ResolutionType)
                {
                    case ResolutionType.Unmodified:
                        throw new Exception("Queries cannot have unmodified resolution. May change in the future.");
                    case ResolutionType.ToList:
                        results = transformed.ToList().Select(o => MapResults(o, query.Selections.Values())).ToList();
                        break;
                    case ResolutionType.FirstOrDefault:
                        results = MapResults(transformed.FirstOrDefault(), query.Selections.Values());
                        break;
                    case ResolutionType.First:
                        results = MapResults(transformed.First(), query.Selections.Values());
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                return results;
            }
            else
            {
                var funcExpr = field.GetExpression(query.Arguments.Values());
                var replaced = (LambdaExpression)ParameterReplacer.Replace(funcExpr, funcExpr.Parameters[0], GraphQLSchema<TContext>.DbParam);
                var selector = GetSelector(field.FieldCLRType, field.Type, query.Selections.Values());
                var invocation = Expression.Invoke(selector, replaced.Body);
                var expr = Expression.Lambda(invocation, GraphQLSchema<TContext>.DbParam);
                var result = expr.Compile().DynamicInvoke(context);
                return MapResults(result, query.Selections.Values());
            }
        }

        private static IDictionary<string, object> MapResults(object queryObject, IEnumerable<ExecSelection<Info>> selections)
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
                    : type.GetProperty(field.Name).GetGetMethod().Invoke(queryObject, new object[] {});

                if (field.IsPost && map.Selections.Any())
                {
                    var selector = GetSelector(field.Type.CLRType, field.Type, map.Selections.Values());
                    obj = selector.Compile().DynamicInvoke(obj);
                }

                if (map.Selections.Any())
                {
                    var listObj = obj as IEnumerable<object>;
                    if (listObj != null)
                    {
                        dict.Add(key, listObj.Select(o => MapResults(o, map.Selections.Values())).ToList());
                    }
                    else if (obj != null)
                    {
                        dict.Add(key, MapResults(obj, map.Selections.Values()));
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

        private static LambdaExpression GetSelector(Type entityType, GraphQLType gqlType, IEnumerable<ExecSelection<Info>> selections)
        {
            var parameter = Expression.Parameter(entityType, "p");
            var init = GetMemberInit(gqlType.QueryType, selections, parameter);
            return Expression.Lambda(init, parameter);
        }

        private static MemberInitExpression GetMemberInit(Type queryType, IEnumerable<ExecSelection<Info>> selections, Expression baseBindingExpr)
        {
            var bindings = selections
                .Where(m => !m.SchemaField.Field().IsPost)
                .Select((map, i) => GetBinding(map, queryType, baseBindingExpr, i + 1)).ToList();

//            bindings.AddRange(Enumerable.Range(bindings.Count + 1, fieldCount - bindings.Count).Select(i => GetEmptyBinding(toType, i)));
            return Expression.MemberInit(Expression.New(queryType), bindings);
        }

        // Stupid EF limitation
        private static MemberBinding GetEmptyBinding(Type toType, int n)
        {
            return Expression.Bind(toType.GetMember($"Field{n}")[0], Expression.Constant(0));
        }

        private static MemberBinding GetBinding(ExecSelection<Info> map, Type toType, Expression baseBindingExpr, int n)
        {
            var field = map.SchemaField.Field();
            var toMember = toType.GetMember(map.SchemaField.FieldName)[0];
            // expr is form of: (context, entity) => entity.Field
            var expr = field.GetExpression(map.Arguments.Values());

            // Replace (entity) with baseBindingExpr, note expression is no longer a LambdaExpression
            // `(context, entity) => entity.Field` becomes `someOtherEntity.Entity.Field` where baseBindingExpr is `someOtherEntity.Entity`
            var replacedBase = ParameterReplacer.Replace(expr.Body, expr.Parameters[1], baseBindingExpr);

            // This just makes sure that the (context) parameter is the same as the one used by the whole query
            var replacedContext = ParameterReplacer.Replace(replacedBase, expr.Parameters[0], GraphQLSchema<TContext>.DbParam);

            // If there aren't any children, then we can assume that this is a scalar entity and we don't have to map child fields
            if (!map.Selections.Any())
                return Expression.Bind(toMember, replacedContext);

            // If binding a single entity, just use the already built selector expression (replaced context)
            // Otherwise, if binding to a list, introduce a new parameter that will be used in a call to .Select
            var listParameter = Expression.Parameter
                (field.Type.CLRType, field.Type.CLRType.Name.Substring(0, 1).ToLower()); // TODO: Name conflicts in expressions?
            var bindChildrenTo = map.SchemaField.Field().IsList ? listParameter : replacedContext;

            // Now that we have our new binding parameter, build the tree for the rest of the children
            var memberInit = GetMemberInit(field.Type.QueryType, map.Selections.Values(), bindChildrenTo);

            // For single entities, we're done and we can just bind to the memberInit expression
            if (!field.IsList)
                return Expression.Bind(toMember, memberInit);

            // However for lists, we need to call .Select() and .ToList() first
            var selectLambda = Expression.Lambda(memberInit, listParameter);
            var call = Expression.Call(typeof (Enumerable), "Select", new[] { field.Type.CLRType, field.Type.QueryType}, replacedContext, selectLambda);
            var toList = Expression.Call(typeof (Enumerable), "ToList", new[] { field.Type.QueryType}, call);
            return Expression.Bind(toMember, toList);
        }
    }
}
