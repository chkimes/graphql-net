using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace GraphQL.Net
{
    public class GraphQLSchema<TContext>
    {
        internal readonly Func<TContext> ContextCreator;
        private readonly List<GraphQLType> _types = GetPrimitives().ToList();
        private readonly List<GraphQLQueryBase<TContext>> _queries = new List<GraphQLQueryBase<TContext>>();

        public static readonly ParameterExpression DbParam = Expression.Parameter(typeof (TContext), "db");

        public GraphQLSchema(Func<TContext> contextCreator)
        {
            ContextCreator = contextCreator;
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

        // This signature is pretty complicated, but necessarily so.
        // We need to build a function that we can execute against passed in TArgs that
        // will return a base expression for combining with selectors (stored on GraphQLType.Fields)
        // This used to be a Func<TContext, TArgs, IQueryable<TEntity>>, i.e. a function that returned a queryable given a context and arguments.
        // However, this wasn't good enough since we needed to be able to reference the (db) parameter in the expressions.
        // For example, being able to do:
        //     db.Users.Select(u => new {
        //         TotalFriends = db.Friends.Count(f => f.UserId = u.Id)
        //     })
        // This meant that the (db) parameter in db.Friends had to be the same (db) parameter in db.Users
        // The only way to do this is to generate the entire expression, i.e. starting with db.Users...
        // The type of that expression is the same as the type of our original Func, but wrapped in Expression<>, so:
        //    Expression<TQueryFunc> where TQueryFunc = Func<TContext, IQueryable<TEntity>>
        // Since the query will change based on arguments, we need a function to generate the above Expression
        // based on whatever arguments are passed in, so:
        //    Func<TArgs, Expression<TQueryFunc>> where TQueryFunc = Func<TContext, IQueryable<TEntity>>
        internal void AddQueryInternal<TArgs, TEntity>(string name, Func<TArgs, Expression<Func<TContext, IQueryable<TEntity>>>> exprGetter, ResolutionType type)
        {
            if (FindQuery(name) != null)
                throw new Exception($"Query named {name} has already been created.");
            _queries.Add(new GraphQLQuery<TContext, TArgs, TEntity>
            {
                Name = name,
                Type = GetGQLType(typeof(TEntity)),
                QueryableExprGetter = exprGetter,
                Schema = this,
                ResolutionType = type,
                ContextCreator = ContextCreator
            });
        }

        internal void AddUnmodifiedQueryInternal<TArgs, TEntity>(string name, Func<TArgs, Expression<Func<TContext, TEntity>>> exprGetter)
        {
            if (FindQuery(name) != null)
                throw new Exception($"Query named {name} has already been created.");
            _queries.Add(new GraphQLQuery<TContext, TArgs, TEntity>
            {
                Name = name,
                Type = GetGQLType(typeof(TEntity)),
                ExprGetter = exprGetter,
                Schema = this,
                ResolutionType = ResolutionType.Unmodified,
                ContextCreator = ContextCreator
            });
        }

        internal GraphQLQueryBase<TContext> FindQuery(string name) => _queries.FirstOrDefault(q => q.Name == name);

        internal GraphQLType GetGQLType(Type type) => GetGQLType(type, _types);
        private static GraphQLType GetGQLType(Type type, List<GraphQLType> types) => types.First(t => t.CLRType == type);

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
}
