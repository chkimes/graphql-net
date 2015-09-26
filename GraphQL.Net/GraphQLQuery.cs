using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;

namespace GraphQL.Net
{
    internal abstract class GraphQLQueryBase<TContext>
    {
        public string Name { get; set; }
        public GraphQLType Type { get; set; }
        public bool List { get; set; }
        public abstract IDictionary<string, object> Execute(Query query);
        public abstract IDictionary<string, object> Execute(TContext context, Query query);
        public Func<TContext> ContextCreator { get; set; }
    }

    internal class GraphQLQuery<TContext, TArgs, TEntity> : GraphQLQueryBase<TContext>
    {
        public Func<TArgs, Expression<Func<TContext, IQueryable<TEntity>>>> ExprGetter { get; set; }
        //public Func<TContext, TArgs, IQueryable<TEntity>> GetQueryable { get; set; }

        public override IDictionary<string, object> Execute(Query query)
        {
            var context = ContextCreator();
            var results = Execute(context, query);
            (context as IDisposable)?.Dispose();
            return results;
        }

        public override IDictionary<string, object> Execute(TContext context, Query query)
        {
            var args = TypeHelpers.GetArgs<TArgs>(query.Inputs);
            var queryableFuncExpr = ExprGetter(args);
            var replaced = (Expression<Func<TContext, IQueryable<TEntity>>>)ParameterReplacer.Replace(queryableFuncExpr, queryableFuncExpr.Parameters[0], GraphQLSchema<TContext>.DbParam);
            var queryable = replaced.Compile()(context); // TODO: Don't do this
            //var queryable = GetQueryable(context, args);
            var fieldMaps = query.Fields.Select(f => MapField(f, Type)).ToList();
            var selector = GetSelector(fieldMaps);

            var selectorExpr = Expression.Quote(selector);
            //var selectMethod = typeof(Queryable).GetMethods(BindingFlags.Static|BindingFlags.Public).First(m => m.Name == "Select" && m.GetParameters().Count() == 2);
            //var closedGenericSelectMethod = selectMethod.MakeGenericMethod(typeof(TItem), typeof(GQLQueryObject));
            var call = Expression.Call(typeof(Queryable), "Select", new[] { typeof(TEntity), typeof(GQLQueryObject) }, replaced.Body, selectorExpr);
            //var call = Expression.Call(null, closedGenericSelectMethod, baseExpr.Body, selector);
            var expr = (Expression<Func<TContext, IQueryable<GQLQueryObject>>>)Expression.Lambda(call, GraphQLSchema<TContext>.DbParam);
            var transformed = expr.Compile()(context);

//            var transformed = (IQueryable<GQLQueryObject>)Queryable.Select(queryable, (dynamic)selector);
            if (!List)
                transformed = transformed.Take(1);
            var data = transformed.ToList();

            var results = data.Select(o => MapResults(o, fieldMaps));

            return new Dictionary<string, object> { {"data", results } };
        }

        private static IDictionary<string, object> MapResults(GQLQueryObject gqlQueryObject, IEnumerable<FieldMap> fieldMaps)
        {
            var n = 1;
            var dict = new Dictionary<string, object>();
            foreach (var map in fieldMaps)
            {
                var key = map.ParsedField.Alias;
                var mappedFieldName = $"Field{n}";
                var obj = typeof(GQLQueryObject).GetProperty(mappedFieldName).GetGetMethod().Invoke(gqlQueryObject, new object[] {});
                var queryObj = obj as GQLQueryObject;
                if (map.Children.Any() && queryObj != null)
                {
                    dict.Add(key, MapResults(queryObj, map.Children));
                }
                else
                {
                    dict.Add(key, obj);
                }
                n++;
            }
            return dict;
        }

        private static LambdaExpression GetSelector(List<FieldMap> fieldMaps)
        {
            var parameter = Expression.Parameter(typeof(TEntity), "p");
            var init = GetMemberInit(fieldMaps, parameter);
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

        private static MemberInitExpression GetMemberInit(IEnumerable<FieldMap> maps, Expression baseBindingExpr)
        {
            var bindings = maps.Select((map, i) => GetBinding(map, baseBindingExpr, i + 1)).ToList();

            bindings.AddRange(Enumerable.Range(bindings.Count + 1, 20 - bindings.Count).Select(GetEmptyBinding));
            return Expression.MemberInit(Expression.New(typeof(GQLQueryObject)), bindings);
        }

        // Stupid EF limitation
        private static MemberBinding GetEmptyBinding(int n)
        {
            return Expression.Bind(typeof(GQLQueryObject).GetMember($"Field{n}")[0], Expression.Constant(0));
        }

        private static MemberBinding GetBinding(FieldMap map, Expression baseBindingExpr, int n)
        {
            var mapFieldName = $"Field{n}";
            var toMember = typeof (GQLQueryObject).GetMember(mapFieldName)[0];
            var expr = map.SchemaField.GetExpression(new List<Input>()); // TODO: real inputs
            var replacedBase = ParameterReplacer.Replace(expr.Body, expr.Parameters[1], baseBindingExpr);
            var replacedContext = ParameterReplacer.Replace(replacedBase, expr.Parameters[0], GraphQLSchema<TContext>.DbParam);
            if (!map.Children.Any())
                return Expression.Bind(toMember, replacedContext);

            var memberInit = GetMemberInit(map.Children, replacedContext);
            return Expression.Bind(typeof(GQLQueryObject).GetMember(mapFieldName)[0], memberInit);
        }

        private static MemberAssignment GetBindingExpr(MemberInfo fromMember, MemberInfo toMember, Expression parameter)
        {
            return Expression.Bind(toMember, Expression.MakeMemberAccess(parameter, fromMember));
        }
    }

    internal class FieldMap
    {
        public Field ParsedField;
        public GraphQLField SchemaField;
        public List<FieldMap> Children;
    }

    public class GQLQueryObject
    {
        public object Field1 { get; set; }
        public object Field2 { get; set; }
        public object Field3 { get; set; }
        public object Field4 { get; set; }
        public object Field5 { get; set; }
        public object Field6 { get; set; }
        public object Field7 { get; set; }
        public object Field8 { get; set; }
        public object Field9 { get; set; }
        public object Field10 { get; set; }
        public object Field11 { get; set; }
        public object Field12 { get; set; }
        public object Field13 { get; set; }
        public object Field14 { get; set; }
        public object Field15 { get; set; }
        public object Field16 { get; set; }
        public object Field17 { get; set; }
        public object Field18 { get; set; }
        public object Field19 { get; set; }
        public object Field20 { get; set; }
    }
}
