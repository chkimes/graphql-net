using System;
using System.Linq.Expressions;

namespace GraphQL.Net
{
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