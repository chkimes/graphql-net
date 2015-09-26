using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace GraphQL.Net
{
    public class GraphQLSchema<TContext>
    {
        private readonly Func<TContext> _contextCreator;
        private readonly List<GraphQLType> _types;
        private readonly List<GraphQLQueryBase<TContext>> _queries = new List<GraphQLQueryBase<TContext>>();

        public static readonly ParameterExpression DbParam = Expression.Parameter(typeof (TContext), "db");

        public GraphQLSchema(Func<TContext> contextCreator)
        {
            _contextCreator = contextCreator;
            _types = GetPrimitives().ToList();
        }

        public GraphQLTypeBuilder<TContext, TEntity> AddType<TEntity>(string description = null)
        {
            var type = typeof (TEntity);
            if (_types.Any(t => t.CLRType == type))
                throw new ArgumentException("Type has already been added");

            var gqlType = new GraphQLType(type) {IsScalar = type.IsPrimitive, Description = description ?? ""};
            _types.Add(gqlType);

            return new GraphQLTypeBuilder<TContext, TEntity>(this, gqlType);
        }

        public GraphQLTypeBuilder<TContext, TEntity> GetType<TEntity>()
        {
            var type = _types.FirstOrDefault(t => t.CLRType == typeof (TEntity));
            if (type == null)
                throw new KeyNotFoundException($"Type {typeof(TEntity).FullName} could not be found.");

            return new GraphQLTypeBuilder<TContext, TEntity>(this, type);
        }

        public void AddQuery<TArgs, TEntity>(string name, TArgs argObj, Expression<Func<TContext, TArgs, IQueryable<TEntity>>> queryableGetter, bool list = false)
        {
            AddQuery(name, queryableGetter, list);
        }

        public void AddQuery<TArgs, TEntity>(string name, Expression<Func<TContext, TArgs, IQueryable<TEntity>>> queryableGetter, bool list = false)
        {
            // TODO: Replace db param here?
            var innerLambda = Expression.Lambda<Func<TContext, IQueryable<TEntity>>>(queryableGetter.Body, queryableGetter.Parameters[0]);
            var quoted = Expression.Quote(innerLambda);
            var outerLambda = Expression.Lambda<Func<TArgs, Expression<Func<TContext, IQueryable<TEntity>>>>>(quoted, queryableGetter.Parameters[1]);
            var exprGetter = outerLambda.Compile();
            AddQuery(name, exprGetter, list);
        }

        public void AddQuery<TEntity>(string name, Expression<Func<TContext, IQueryable<TEntity>>> queryableGetter, bool list = false)
        {
            // TODO: Replace db param here?
            var quoted = Expression.Quote(queryableGetter);
            var outerLambda = Expression.Lambda<Func<object, Expression<Func<TContext, IQueryable<TEntity>>>>>(quoted, Expression.Parameter(typeof(object), "o"));
            var exprGetter = outerLambda.Compile();
            AddQuery(name, exprGetter, list);
        }

        private void AddQuery<TArgs, TEntity>(string name, Func<TArgs, Expression<Func<TContext, IQueryable<TEntity>>>> exprGetter, bool list)
        {
            _queries.Add(new GraphQLQuery<TContext, TArgs, TEntity>
            {
                Name = name,
                Type = GetGQLType(typeof(TEntity)),
                ExprGetter = exprGetter,
                List = list,
                ContextCreator = _contextCreator
            });
        }

        internal GraphQLQueryBase<TContext> FindQuery(string name)
        {
            // TODO: Name duplicates?
            return _queries.FirstOrDefault(q => q.Name == name);
        }

        internal GraphQLType GetGQLType(Type type)
        {
            return GetGQLType(type, _types);
        }

        private static GraphQLType GetGQLType(Type type, List<GraphQLType> types)
        {
            return types.First(t => t.CLRType == type);
        }

        private static IEnumerable<GraphQLType> GetPrimitives()
        {
            return new[]
            {
                new GraphQLType(typeof(int)) { IsScalar = true },
                new GraphQLType(typeof(float)) { IsScalar = true },
                new GraphQLType(typeof(string)) { IsScalar = true },
                new GraphQLType(typeof(bool)) { IsScalar = true },
                new GraphQLType(typeof(Guid)) { Name = "ID", IsScalar = true }
            };
        }
    }

    public class GraphQLTypeBuilder<TContext, TEntity>
    {
        private readonly GraphQLSchema<TContext> _schema;
        private readonly GraphQLType _type;

        internal GraphQLTypeBuilder(GraphQLSchema<TContext> schema, GraphQLType type)
        {
            _schema = schema;
            _type = type;
        }

        public GraphQLTypeBuilder<TContext, TEntity> AddField<TArgs, TField>(string name, TArgs shape, Func<TArgs, Expression<Func<TContext, TEntity, TField>>> exprFunc)
        {
            _type.Fields.Add(new GraphQLField<TContext, TArgs, TEntity, TField>(_schema)
                             {
                                 Name = name,
                                 ExprFunc = exprFunc
                             });
            return this;
        }

        public GraphQLTypeBuilder<TContext, TEntity> AddField<TField>(Expression<Func<TEntity, TField>> expr)
        {
            var member = expr.Body as MemberExpression;
            if (member == null)
                throw new InvalidOperationException($"{nameof(expr)} must be a MemberExpression of form [p => p.Field]");
            var name = member.Member.Name;
            var lambda = Expression.Lambda<Func<TContext, TEntity, TField>>(member, GraphQLSchema<TContext>.DbParam, expr.Parameters[0]);
            return AddField(name.ToCamelCase(), lambda);
        }

        public GraphQLTypeBuilder<TContext, TEntity> AddField<TField>(string name, Expression<Func<TContext, TEntity, TField>> expr)
            => AddField(name, new object(), o => expr);

        public GraphQLTypeBuilder<TContext, TEntity> AddAllFields()
        {
            throw new NotImplementedException();
        }
    }
}
