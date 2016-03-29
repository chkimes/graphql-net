using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace GraphQL.Net
{
    internal static class Executor<TContext>
    {
        public static IDictionary<string, object> Execute<TArgs, TEntity>(GraphQLSchema<TContext> schema, GraphQLQuery<TContext, TArgs, TEntity> gqlQuery, Query query)
        {
            var context = schema.ContextCreator();
            var results = Execute(context, gqlQuery, query);
            (context as IDisposable)?.Dispose();
            return results;
        }

        public static IDictionary<string, object> Execute<TArgs, TEntity>(TContext context, GraphQLQuery<TContext, TArgs, TEntity> gqlQuery, Query query)
        {
            var args = TypeHelpers.GetArgs<TArgs>(query.Inputs);
            var queryableFuncExpr = gqlQuery.QueryableExprGetter(args);
            var replaced = (Expression<Func<TContext, IQueryable<TEntity>>>)ParameterReplacer.Replace(queryableFuncExpr, queryableFuncExpr.Parameters[0], GraphQLSchema<TContext>.DbParam);
            var fieldMaps = query.Fields.Select(f => MapField(f, gqlQuery.Type)).ToList();
            var selector = GetSelector<TEntity>(gqlQuery.Type, fieldMaps);

            var selectorExpr = Expression.Quote(selector);
            var call = Expression.Call(typeof(Queryable), "Select", new[] { typeof(TEntity), gqlQuery.Type.QueryType }, replaced.Body, selectorExpr);
            var expr = Expression.Lambda(call, GraphQLSchema<TContext>.DbParam);
            var transformed = (IQueryable<object>)expr.Compile().DynamicInvoke(context);

            object results;
            switch (gqlQuery.ResolutionType)
            {
                case ResolutionType.Unmodified:
                    throw new Exception("Queries cannot have unmodified resolution. May change in the future.");
                case ResolutionType.ToList:
                    results = transformed.ToList().Select(o => MapResults(o, fieldMaps)).ToList();
                    break;
                case ResolutionType.FirstOrDefault:
                    results = MapResults(transformed.FirstOrDefault(), fieldMaps);
                    break;
                case ResolutionType.First:
                    results = MapResults(transformed.First(), fieldMaps);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return new Dictionary<string, object> {{"data", results}};
        }

        private static IDictionary<string, object> MapResults(object queryObject, IEnumerable<FieldMap> fieldMaps)
        {
            if (queryObject == null) // TODO: Check type non-null and throw exception
                return null;
            var n = 1;
            var dict = new Dictionary<string, object>();
            var type = queryObject.GetType();
            foreach (var map in fieldMaps)
            {
                var key = map.ParsedField.Alias;
                var obj = type.GetProperty(map.SchemaField.Name).GetGetMethod().Invoke(queryObject, new object[] { });
                if (map.Children.Any())
                {
                    var listObj = obj as IEnumerable<object>;
                    if (listObj != null)
                    {
                        dict.Add(key, listObj.Select(o => MapResults(o, map.Children)).ToList());
                    }
                    else if (obj != null)
                    {
                        dict.Add(key, MapResults(obj, map.Children));
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
                n++;
            }
            return dict;
        }

        private static LambdaExpression GetSelector<TEntity>(GraphQLType gqlType, List<FieldMap> fieldMaps)
        {
            var parameter = Expression.Parameter(typeof(TEntity), "p");
            var init = GetMemberInit(gqlType.QueryType, fieldMaps, parameter);
            return Expression.Lambda(init, parameter);
        }

        private static FieldMap MapField(Field field, GraphQLType type)
        {
            var schemaField = type.Fields.First(f => f.Name == field.Name);
            return new FieldMap
            {
                ParsedField = field,
                SchemaField = schemaField,
                Children = field.Fields.Select(f => MapField(f, schemaField.Type)).ToList(),
            };
        }

        private static MemberInitExpression GetMemberInit(Type queryType, IList<FieldMap> maps, Expression baseBindingExpr)
        {
            var bindings = maps.Select((map, i) => GetBinding(map, queryType, baseBindingExpr, i + 1)).ToList();

//            bindings.AddRange(Enumerable.Range(bindings.Count + 1, fieldCount - bindings.Count).Select(i => GetEmptyBinding(toType, i)));
            return Expression.MemberInit(Expression.New(queryType), bindings);
        }

        // Stupid EF limitation
        private static MemberBinding GetEmptyBinding(Type toType, int n)
        {
            return Expression.Bind(toType.GetMember($"Field{n}")[0], Expression.Constant(0));
        }

        private static MemberBinding GetBinding(FieldMap map, Type toType, Expression baseBindingExpr, int n)
        {
            var toMember = toType.GetMember(map.SchemaField.Name)[0];
            // expr is form of: (context, entity) => entity.Field
            var expr = map.SchemaField.GetExpression(new List<Input>()); // TODO: real inputs

            // Replace (entity) with baseBindingExpr, note expression is no longer a LambdaExpression
            // `(context, entity) => entity.Field` becomes `someOtherEntity.Entity.Field` where baseBindingExpr is `someOtherEntity.Entity`
            var replacedBase = ParameterReplacer.Replace(expr.Body, expr.Parameters[1], baseBindingExpr);

            // This just makes sure that the (context) parameter is the same as the one used by the whole query
            var replacedContext = ParameterReplacer.Replace(replacedBase, expr.Parameters[0], GraphQLSchema<TContext>.DbParam);

            // If there aren't any children, then we can assume that this is a scalar entity and we don't have to map child fields
            if (!map.Children.Any())
                return Expression.Bind(toMember, replacedContext);

            // If binding a single entity, just use the already built selector expression (replaced context)
            // Otherwise, if binding to a list, introduce a new parameter that will be used in a call to .Select
            var listParameter = Expression.Parameter(map.SchemaField.Type.CLRType, map.SchemaField.Type.CLRType.Name.Substring(0, 1).ToLower()); // TODO: Name conflicts in expressions?
            var bindChildrenTo = map.SchemaField.IsList ? listParameter : replacedContext;

            // Now that we have our new binding parameter, build the tree for the rest of the children
            var memberInit = GetMemberInit(map.SchemaField.Type.QueryType, map.Children, bindChildrenTo);

            // For single entities, we're done and we can just bind to the memberInit expression
            if (!map.SchemaField.IsList)
                return Expression.Bind(toMember, memberInit);

            // However for lists, we need to call .Select() and .ToList() first
            var selectLambda = Expression.Lambda(memberInit, listParameter);
            var call = Expression.Call(typeof (Enumerable), "Select", new[] {map.SchemaField.Type.CLRType, map.SchemaField.Type.QueryType}, replacedContext, selectLambda);
            var toList = Expression.Call(typeof (Enumerable), "ToList", new[] {map.SchemaField.Type.QueryType}, call);
            return Expression.Bind(toMember, toList);
        }
    }

    internal class FieldMap
    {
        public Field ParsedField;
        public GraphQLField SchemaField;
        public List<FieldMap> Children;
    }
}
