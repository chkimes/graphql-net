using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace EntityFramework.GraphQL
{
    public class Executor<TContext> where TContext : DbContext, new()
    {
        private List<QuerySchema> _queries;

        public object Execute(Query query)
        {
            EnsureQueriesPopulated();
            var schema = _queries.First(p => p.Name == query.Name.ToLower());
            var expr = GetSelectorExpr(query.Fields, schema.Type);
            using (var db = new TContext())
            {
                var queryable = (dynamic)schema.QueryableGetter(db);
                if (schema.WhereExpression != null)
                    queryable = Queryable.Where(queryable, (dynamic)schema.WhereExpression);
                queryable = Queryable.Select(queryable, (dynamic)expr);
                return Enumerable.ToList(queryable);
            }
        }

        private void EnsureQueriesPopulated()
        {
            if (_queries != null)
                return;
            _queries = GetQueries();
        }

        private class QuerySchema
        {
            public string Name;
            public Type Type;
            public LambdaExpression WhereExpression;
            public Func<TContext, IQueryable> QueryableGetter;
        }

        private static List<QuerySchema> GetQueries()
        {
            return typeof(TContext)
                .GetProperties()
                .Where(p => p.PropertyType.IsGenericType
                    && TypeHelpers.IsAssignableToGenericType(p.PropertyType, typeof(IDbSet<>)))
                .Select(p => new QuerySchema
                {
                    Name = p.PropertyType.GetGenericArguments()[0].Name.ToLower(),
                    Type = p.PropertyType.GetGenericArguments()[0],
                    WhereExpression = null,
                    QueryableGetter = (Func<TContext, IQueryable>)Delegate.CreateDelegate(typeof(Func<TContext, IQueryable>), p.GetGetMethod())
                })
                .ToList();
        }

        private static DynamicTypeBuilder _dBuilder = new DynamicTypeBuilder();
        private static LambdaExpression GetSelectorExpr(IEnumerable<Field> fields, Type fromType)
        {
            var parameter = Expression.Parameter(fromType, "p");
            var infos = GetMemberMapInfo(fields, fromType);
            var init = GetMemberInit(infos.Item1, fromType, infos.Item2, parameter);
            return Expression.Lambda(init, parameter);
        }

        private class MemberMapInfo
        {
            public PropertyInfo Prop;
            public Field Field;
            public Type AssignmentType;
            public MemberAssignment Binding;
            public List<MemberMapInfo> Children;
        }

        private static MemberInitExpression GetMemberInit(IEnumerable<MemberMapInfo> members, Type fromType, Type toType, Expression baseBindingExpr)
        {
            foreach (var info in members)
            {
                if (!IsGQLPrimitive(info.Prop.PropertyType))
                {
                    var selector = Expression.MakeMemberAccess(baseBindingExpr, info.Prop);
                    var memberInit = GetMemberInit(info.Children, info.Prop.PropertyType, info.AssignmentType, selector);
                    var bindingExpression = Expression.Bind(toType.GetMember(info.Field.Alias)[0], memberInit);
                    info.Binding = bindingExpression;
                }
                else
                {
                    info.Binding = GetBindingExpr(info.Prop, toType.GetMember(info.Field.Alias)[0], baseBindingExpr);
                }
            }

            var bindings = members.Select(f => f.Binding).ToArray();
            return Expression.MemberInit(Expression.New(toType), bindings);
        }

        private static Tuple<List<MemberMapInfo>, Type> GetMemberMapInfo(IEnumerable<Field> fields, Type fromType)
        {
            if (!fields.Any())
                throw new Exception("Must specify at least 1 field");
            var infos = new List<MemberMapInfo>();
            var members = fromType.GetProperties().ToDictionary(p => p.Name.ToLower());
            foreach (var field in fields)
            {
                var info = new MemberMapInfo();
                info.Prop = members[field.Name.ToLower()];
                info.Field = field;
                if (IsGQLPrimitive(info.Prop.PropertyType))
                {
                    info.AssignmentType = info.Prop.PropertyType;
                }
                else
                {
                    var childInfos = GetMemberMapInfo(field.Fields, info.Prop.PropertyType);
                    info.Children = childInfos.Item1;
                    info.AssignmentType = childInfos.Item2;
                }
                infos.Add(info);
            }

            var dType = _dBuilder.CreateDynamicType(Guid.NewGuid().ToString(), infos.ToDictionary(i => i.Field.Alias, i => i.AssignmentType));
            return Tuple.Create(infos, dType);
        }

        private static bool IsGQLPrimitive(Type type)
        {
            return type == typeof(int) || type == typeof(float) || type == typeof(string) || type == typeof(bool);
        }

        private static MemberAssignment GetBindingExpr(MemberInfo fromMember, MemberInfo toMember, Expression parameter)
        {
            return Expression.Bind(toMember, Expression.MakeMemberAccess(parameter, fromMember));
        }
    }
}
