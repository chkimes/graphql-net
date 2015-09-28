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

        // This overload is provided to the user so they can shape TArgs with an anonymous type and rely on type inference for type parameters
        // e.g.  AddQuery("user", new { id = 0 }, (db, args) => db.Users.Where(u => u.Id == args.id));
        public void AddQuery<TArgs, TEntity>(string name, TArgs argObj, Expression<Func<TContext, TArgs, IQueryable<TEntity>>> queryableGetter)
            => AddQuery(name, queryableGetter);

        public void AddLookup<TArgs, TEntity>(string name, TArgs argObj, Expression<Func<TContext, TArgs, IQueryable<TEntity>>> queryableGetter)
            => AddLookup(name, queryableGetter);

        private void AddQuery<TArgs, TEntity>(string name, Expression<Func<TContext, TArgs, IQueryable<TEntity>>> queryableGetter, bool list)
        {
            // TODO: Replace db param here?
            // Transform  (db, args) => db.Entities.Where(args)  into  args => db => db.Entities.Where(args)
            var innerLambda = Expression.Lambda<Func<TContext, IQueryable<TEntity>>>(queryableGetter.Body, queryableGetter.Parameters[0]);
            var quoted = Expression.Quote(innerLambda);
            var outerLambda = Expression.Lambda<Func<TArgs, Expression<Func<TContext, IQueryable<TEntity>>>>>(quoted, queryableGetter.Parameters[1]);
            var exprGetter = outerLambda.Compile();
            AddQuery(name, exprGetter, list);
        }

        public void AddQuery<TArgs, TEntity>(string name, Expression<Func<TContext, TArgs, IQueryable<TEntity>>> queryableGetter)
            => AddQuery(name, queryableGetter, true);

        public void AddLookup<TArgs, TEntity>(string name, Expression<Func<TContext, TArgs, IQueryable<TEntity>>> queryableGetter)
            => AddQuery(name, queryableGetter, false);

        private void AddQuery<TEntity>(string name, Expression<Func<TContext, IQueryable<TEntity>>> queryableGetter, bool list)
        {
            // TODO: Replace db param here?
            // Transform  db => db.Entities.Where(args)  into  args => db => db.Entities.Where(args)
            var quoted = Expression.Quote(queryableGetter);
            var outerLambda = Expression.Lambda<Func<object, Expression<Func<TContext, IQueryable<TEntity>>>>>(quoted, Expression.Parameter(typeof(object), "o"));
            var exprGetter = outerLambda.Compile();
            AddQuery(name, exprGetter, list);
        }

        public void AddQuery<TEntity>(string name, Expression<Func<TContext, IQueryable<TEntity>>> queryableGetter)
            => AddQuery(name, queryableGetter, true);

        public void AddLookup<TEntity>(string name, Expression<Func<TContext, IQueryable<TEntity>>> queryableGetter)
            => AddQuery(name, queryableGetter, false);

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
        private void AddQuery<TArgs, TEntity>(string name, Func<TArgs, Expression<Func<TContext, IQueryable<TEntity>>>> exprGetter, bool list)
        {
            if (FindQuery(name) != null)
                throw new Exception($"Query named {name} has already been created.");
            _queries.Add(new GraphQLQuery<TContext, TArgs, TEntity>
            {
                Name = name,
                Type = GetGQLType(typeof(TEntity)),
                ExprGetter = exprGetter,
                Schema = this,
                List = list,
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

    public class GraphQLTypeBuilder<TContext, TEntity>
    {
        private readonly GraphQLSchema<TContext> _schema;
        private readonly GraphQLType _type;

        internal GraphQLTypeBuilder(GraphQLSchema<TContext> schema, GraphQLType type)
        {
            _schema = schema;
            _type = type;
        }

        // This overload is provided to the user so they can shape TArgs with an anonymous type and rely on type inference for type parameters
        // e.g.  AddField("profilePic", new { size = 0 }, (db, user) => db.ProfilePics.Where(p => p.UserId == u.Id && p.Size == args.size));
        public GraphQLTypeBuilder<TContext, TEntity> AddField<TArgs, TField>(string name, TArgs shape, Func<TArgs, Expression<Func<TContext, TEntity, TField>>> exprFunc)
            => AddField(name, exprFunc);

        // See GraphQLSchema.AddQuery for an explanation of the type of exprFunc, since it follows similar reasons
        // TL:DR; Fields can have parameters passed in, so the Expression<Func> to be used is dependent on TArgs
        //        Fields can use TContext as well, so we have to return an Expression<Func<TContext, TEntity, TField>> and replace the TContext parameter when needed
        public GraphQLTypeBuilder<TContext, TEntity> AddField<TArgs, TField>(string name, Func<TArgs, Expression<Func<TContext, TEntity, TField>>> exprFunc)
        {
            _type.Fields.Add(new GraphQLField<TContext, TArgs, TEntity, TField>(_schema, name, exprFunc));
            return this;
        }

        // Overload provided for easily adding properties, e.g.  AddField(u => u.Name);
        public GraphQLTypeBuilder<TContext, TEntity> AddField<TField>(Expression<Func<TEntity, TField>> expr)
        {
            var member = expr.Body as MemberExpression;
            if (member == null)
                throw new InvalidOperationException($"{nameof(expr)} must be a MemberExpression of form [p => p.Field]");
            var name = member.Member.Name;
            var lambda = Expression.Lambda<Func<TContext, TEntity, TField>>(member, GraphQLSchema<TContext>.DbParam, expr.Parameters[0]);
            return AddField(name.ToCamelCase(), lambda);
        }

        // Overload provided for adding fields with no arguments, e.g.  AddField("totalCount", (db, u) => db.Users.Count());
        public GraphQLTypeBuilder<TContext, TEntity> AddField<TField>(string name, Expression<Func<TContext, TEntity, TField>> expr)
            => AddField(name, new object(), o => expr);

        public GraphQLTypeBuilder<TContext, TEntity> AddAllFields()
        {
            throw new NotImplementedException();
        }
    }
}
