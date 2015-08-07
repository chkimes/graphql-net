using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace EntityFramework.GraphQL
{
    public abstract class GraphQLQueryBase<TContext>
    {
        public string Name { get; set; }
        public GraphQLType Type { get; set; }
        public List<InputValue> Arguments { get; set; }
        public bool List { get; set; }
        public abstract IDictionary<string, object> Execute(TContext context, Query query);
    }

    public class GraphQLQuery<TContext, TArgs, TEntity> : GraphQLQueryBase<TContext>
    {
        public Func<TContext, TArgs, IQueryable<TEntity>> GetQueryable { get; set; }

        public override IDictionary<string, object> Execute(TContext context, Query query)
        {
            var args = GetArgs(query.Inputs);
            var queryable = GetQueryable(context, args);
            var fieldMaps = query.Fields.Select(f => MapField(f, Type)).ToList();
            var selector = GetSelector(fieldMaps);
            var transformed = (IQueryable<GQLQueryObject>)Queryable.Select(queryable, (dynamic)selector);
            if (!List)
                transformed = transformed.Take(1);
            var data = transformed.ToList();

            var results = data.Select(o => MapResults(o, fieldMaps));

            return new Dictionary<string, object> { {"data", results } };
        }

        private IDictionary<string, object> MapResults(GQLQueryObject gqlQueryObject, List<FieldMap> fieldMaps)
        {
            var n = 1;
            var dict = new Dictionary<string, object>();
            foreach (var map in fieldMaps)
            {
                var key = map.ParsedField.Alias;
                var mappedFieldName = $"Field{n}";
                var obj = ToType.GetProperty(mappedFieldName).GetGetMethod().Invoke(gqlQueryObject, new object[] {});
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

        private LambdaExpression GetSelector(List<FieldMap> fieldMaps)
        {
            var parameter = Expression.Parameter(typeof(TEntity), "p");
            var init = GetMemberInit(fieldMaps, parameter);
            return Expression.Lambda(init, parameter);
        }

        private class FieldMap
        {
            public Field ParsedField;
            public GraphQLField SchemaField;
            public List<FieldMap> Children;
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

        private static readonly Type ToType = typeof (GQLQueryObject);
        private static MemberInitExpression GetMemberInit(IEnumerable<FieldMap> maps, Expression baseBindingExpr)
        {
            var bindings = maps.Select((map, i) => GetBinding(map, baseBindingExpr, i + 1)).ToList();

            bindings.AddRange(Enumerable.Range(bindings.Count + 1, 20 - bindings.Count).Select(GetEmptyBinding));
            return Expression.MemberInit(Expression.New(ToType), bindings);
        }

        // Stupid EF limitation
        private static MemberBinding GetEmptyBinding(int n)
        {
            return Expression.Bind(ToType.GetMember($"Field{n}")[0], Expression.Constant(0));
        }

        private static MemberBinding GetBinding(FieldMap map, Expression baseBindingExpr, int n)
        {
            var mapFieldName = $"Field{n}";
            if (!map.Children.Any())
                return GetBindingExpr(map.SchemaField.PropInfo, ToType.GetMember(mapFieldName)[0], baseBindingExpr);

            var selector = Expression.MakeMemberAccess(baseBindingExpr, map.SchemaField.PropInfo);
            var memberInit = GetMemberInit(map.Children, selector);
            return Expression.Bind(ToType.GetMember(mapFieldName)[0], memberInit);
        }

        private static MemberAssignment GetBindingExpr(MemberInfo fromMember, MemberInfo toMember, Expression parameter)
        {
            return Expression.Bind(toMember, Expression.MakeMemberAccess(parameter, fromMember));
        }

        public static TArgs GetArgs(List<Input> inputs)
        {
            var paramlessCtor = typeof(TArgs).GetConstructors().FirstOrDefault(c => c.GetParameters().Length == 0);
            if (paramlessCtor != null)
                return GetParamlessArgs(paramlessCtor, inputs);
            var anonTypeCtor = typeof(TArgs).GetConstructors().Single();
            return GetAnonymousArgs(anonTypeCtor, inputs);
        }

        private static TArgs GetAnonymousArgs(ConstructorInfo anonTypeCtor, List<Input> inputs)
        {
            var parameters = anonTypeCtor
                .GetParameters()
                .Select(p => GetParameter(p, inputs))
                .ToArray();
            return (TArgs)anonTypeCtor.Invoke(parameters);
        }

        private static object GetParameter(ParameterInfo param, List<Input> inputs)
        {
            var input = inputs.FirstOrDefault(i => i.Name == param.Name);
            return input != null ? input.Value : TypeHelpers.GetDefault(param.ParameterType);
        }

        private static TArgs GetParamlessArgs(ConstructorInfo paramlessCtor, List<Input> inputs)
        {
            var args = (TArgs)paramlessCtor.Invoke(null);
            foreach (var input in inputs)
                typeof(TArgs).GetProperty(input.Name).GetSetMethod().Invoke(args, new[] { input.Value });
            return args;
        }
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
