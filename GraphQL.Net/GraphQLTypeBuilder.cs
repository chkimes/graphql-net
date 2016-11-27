using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GraphQL.Net
{
    public class GraphQLTypeBuilder<TContext, TExecutionParameters, TEntity>
    {
        private readonly GraphQLSchema<TContext, TExecutionParameters> _schema;
        private readonly GraphQLType _type;

        internal GraphQLTypeBuilder(GraphQLSchema<TContext, TExecutionParameters> schema, GraphQLType type)
        {
            _schema = schema;
            _type = type;
        }

        // This overload is provided to the user so they can shape TArgs with an anonymous type and rely on type inference for type parameters
        // e.g.  AddField("profilePic", new { size = 0 }, (db, user) => db.ProfilePics.Where(p => p.UserId == u.Id && p.Size == args.size));
        public GraphQLFieldBuilder<TContext, TExecutionParameters, TField> AddField<TArgs, TField>(string name, TArgs shape, Expression<Func<TContext, TArgs, TEntity, TField>> exprFunc)
            => AddFieldInternal(name, AdjustExprFunc(exprFunc));

        public GraphQLFieldBuilder<TContext, TExecutionParameters, TField> AddListField<TArgs, TField>(string name, TArgs shape, Expression<Func<TContext, TArgs, TEntity, IEnumerable<TField>>> exprFunc)
            => AddListFieldInternal(name, AdjustExprFunc(exprFunc));

        public Func<TArgs, TExecutionParameters, Expression<Func<TContext, TEntity, TExecutionParameters, TReturn>>> AdjustExprFunc<TArgs, TReturn>(Expression<Func<TContext, TArgs, TEntity, TExecutionParameters, TReturn>> exprFunc)
        {
            var param1 = exprFunc.Parameters[1];
            var param2 = exprFunc.Parameters[2];
            var transformedExpr = Expression.Lambda(Expression.Convert(exprFunc.Body, typeof(TReturn)), exprFunc.Parameters[0], exprFunc.Parameters[2]);
            var quoted = Expression.Quote(transformedExpr);
            var final = Expression.Lambda<Func<TArgs, TExecutionParameters, Expression<Func<TContext, TEntity, TExecutionParameters, TReturn>>>>(quoted, new List<ParameterExpression> { param1, param2});
            return final.Compile();
        }

        public Func<TArgs, Expression<Func<TContext, TEntity, TReturn>>> AdjustExprFunc<TArgs, TReturn>(Expression<Func<TContext, TArgs, TEntity, TReturn>> exprFunc)
        {
            var param = exprFunc.Parameters[1];
            var transformedExpr = Expression.Lambda(Expression.Convert(exprFunc.Body, typeof(TReturn)), exprFunc.Parameters[0], exprFunc.Parameters[2]);
            var quoted = Expression.Quote(transformedExpr);
            var final = Expression.Lambda<Func<TArgs, Expression<Func<TContext, TEntity, TReturn>>>>(quoted, param);
            return final.Compile();
        }

        // See GraphQLSchema.AddField for an explanation of the type of exprFunc, since it follows similar reasons
        // TL:DR; OwnFields can have parameters passed in, so the Expression<Func> to be used is dependent on TArgs
        //        OwnFields can use TContext as well, so we have to return an Expression<Func<TContext, TEntity, TField>> and replace the TContext parameter when needed
        public GraphQLFieldBuilder<TContext, TExecutionParameters, TField> AddField<TArgs, TField>(string name, Func<TArgs, Expression<Func<TContext, TEntity, TField>>> exprFunc)
            => AddFieldInternal(name, exprFunc);
        
        public GraphQLFieldBuilder<TContext, TExecutionParameters, TField> AddField<TArgs, TField>(string name, Func<TArgs, TExecutionParameters, Expression<Func<TContext, TEntity, TExecutionParameters, TField>>> exprFunc)
            => AddFieldInternal(name, exprFunc);

        public GraphQLFieldBuilder<TContext, TExecutionParameters, TField> AddListField<TArgs, TField>(string name, Func<TArgs, Expression<Func<TContext, TEntity, IEnumerable<TField>>>> exprFunc)
            => AddListFieldInternal(name, exprFunc);

        public GraphQLFieldBuilder<TContext, TExecutionParameters, TField> AddListField<TArgs, TField>(string name, Func<TArgs, TExecutionParameters, Expression<Func<TContext, TEntity, IEnumerable<TField>>>> exprFunc)
            => AddListFieldInternal(name, exprFunc);

        // Mutation should be null UNLESS adding a mutation at the schema level
        internal GraphQLFieldBuilder<TContext, TExecutionParameters, TField> AddFieldInternal<TArgs, TField>(string name, Func<TArgs, TExecutionParameters, Expression<Func<TContext, TEntity, TExecutionParameters, TField>>> exprFunc)
        {
            var field = GraphQLField.New(_schema, name, exprFunc, typeof (TField), _type);
            _type.OwnFields.Add(field);
            return new GraphQLFieldBuilder<TContext, TExecutionParameters, TField>(field);
        }

        // Mutation should be null UNLESS adding a mutation at the schema level
        internal GraphQLFieldBuilder<TContext, TExecutionParameters, TField> AddFieldInternal<TArgs, TField>(string name, Func<TArgs, Expression<Func<TContext, TEntity, TField>>> exprFunc)
        {
            var field = GraphQLField.New(_schema, name, exprFunc, typeof(TField), _type);
            _type.OwnFields.Add(field);
            return new GraphQLFieldBuilder<TContext, TExecutionParameters, TField>(field);
        }

        // Mutation should be null UNLESS adding a mutation at the schema level
        internal GraphQLFieldBuilder<TContext, TExecutionParameters, TField> AddListFieldInternal<TArgs, TField>(string name, Func<TArgs, TExecutionParameters, Expression<Func<TContext, TEntity, IEnumerable<TField>>>> exprFunc)
        {
            var field = GraphQLField.New(_schema, name, exprFunc, typeof (IEnumerable<TField>), _type);
            _type.OwnFields.Add(field);
            return new GraphQLFieldBuilder<TContext, TExecutionParameters, TField>(field);
        }

        // Mutation should be null UNLESS adding a mutation at the schema level
        internal GraphQLFieldBuilder<TContext, TExecutionParameters, TField> AddListFieldInternal<TArgs, TField>(string name, Func<TArgs, Expression<Func<TContext, TEntity, IEnumerable<TField>>>> exprFunc)
        {
            var field = GraphQLField.New(_schema, name, exprFunc, typeof(IEnumerable<TField>), _type);
            _type.OwnFields.Add(field);
            return new GraphQLFieldBuilder<TContext, TExecutionParameters, TField>(field);
        }

        // Mutation should be null UNLESS adding a mutation at the schema level
        internal GraphQLFieldBuilder<TContext, TExecutionParameters, TField> AddMutationInternal<TArgs, TField, TMutReturn>(string name, Func<TArgs, TMutReturn, Expression<Func<TContext, TEntity, TField>>> exprFunc, Func<TContext, TArgs, TExecutionParameters, TMutReturn> mutation)
        {
            var field = GraphQLField.NewMutation(_schema, name, exprFunc, typeof (TField), _type, mutation);
            _type.OwnFields.Add(field);
            return new GraphQLFieldBuilder<TContext, TExecutionParameters, TField>(field);
        }

        // Mutation should be null UNLESS adding a mutation at the schema level
        internal GraphQLFieldBuilder<TContext, TExecutionParameters, TField> AddListMutationInternal<TArgs, TField, TMutReturn>(string name, Func<TArgs, TMutReturn, Expression<Func<TContext, TEntity, IEnumerable<TField>>>> exprFunc, Func<TContext, TArgs, TExecutionParameters, TMutReturn> mutation)
        {
            var field = GraphQLField.NewMutation(_schema, name, exprFunc, typeof (IEnumerable<TField>), _type, mutation);
            _type.OwnFields.Add(field);
            return new GraphQLFieldBuilder<TContext, TExecutionParameters, TField>(field);
        }

        public GraphQLFieldBuilder<TContext, TExecutionParameters, TField> AddField<TField>(string name, Expression<Func<TEntity, TField>> expr)
        {
            var lambda = Expression.Lambda<Func<TContext, TEntity, TField>>(expr.Body, GraphQLSchema<TContext, TExecutionParameters>.DbParam, expr.Parameters[0]);
            return AddField(name.ToCamelCase(), lambda);
        }

        public GraphQLFieldBuilder<TContext, TExecutionParameters, TField> AddListField<TField>(string name, Expression<Func<TEntity, IEnumerable<TField>>> expr)
        {
            var lambda = Expression.Lambda<Func<TContext, TEntity, IEnumerable<TField>>>(expr.Body, GraphQLSchema<TContext, TExecutionParameters>.DbParam, expr.Parameters[0]);
            return AddListField(name.ToCamelCase(), lambda);
        }

        // Overload provided for easily adding properties, e.g.  AddField(u => u.Name);
        public GraphQLFieldBuilder<TContext, TExecutionParameters, TField> AddField<TField>(Expression<Func<TEntity, TField>> expr)
        {
            var member = expr.Body as MemberExpression;
            if (member == null)
                throw new InvalidOperationException($"Unnamed query {nameof(expr)} must be a MemberExpression of form [p => p.Field].\n\nTry using the explicit AddField overload for a custom field.");
            var name = member.Member.Name;
            var lambda = Expression.Lambda<Func<TContext, TEntity, TField>>(member, GraphQLSchema<TContext, TExecutionParameters>.DbParam, expr.Parameters[0]);
            return AddField(name.ToCamelCase(), lambda);
        }

        public GraphQLFieldBuilder<TContext, TExecutionParameters, TField> AddListField<TField>(Expression<Func<TEntity, IEnumerable<TField>>> expr)
        {
            var member = expr.Body as MemberExpression;
            if (member == null)
                throw new InvalidOperationException($"Unnamed query {nameof(expr)} must be a MemberExpression of form [p => p.Field].\n\nTry using the explicit AddField overload for a custom field.");
            var name = member.Member.Name;
            var lambda = Expression.Lambda<Func<TContext, TEntity, IEnumerable<TField>>>(member, GraphQLSchema<TContext, TExecutionParameters>.DbParam, expr.Parameters[0]);
            return AddListField(name.ToCamelCase(), lambda);
        }

        // Overload provided for adding fields with no arguments, e.g.  AddField("totalCount", (db, u) => db.Users.Count());
        public GraphQLFieldBuilder<TContext, TExecutionParameters, TField> AddField<TField>(string name, Expression<Func<TContext, TEntity, TField>> expr)
            => AddField<object, TField>(name, o => expr);

        public GraphQLFieldBuilder<TContext, TExecutionParameters, TField> AddListField<TField>(string name, Expression<Func<TContext, TEntity, IEnumerable<TField>>> expr)
            => AddListField<object, TField>(name, o => expr);

        public void AddAllFields()
        {
            foreach (var prop in typeof (TEntity).GetProperties(BindingFlags.Public | BindingFlags.Instance))
                _type.OwnFields.Add(CreateGenericField(prop));
        }

        // unsafe generic magic to create a GQLField instance
        private GraphQLField CreateGenericField(PropertyInfo prop)
        {
            // build selector expression, e.g.: (db, p) => p.Id
            var entityParam = Expression.Parameter(typeof(TEntity), "p");
            var memberExpr = Expression.MakeMemberAccess(entityParam, prop);
            var lambda = Expression.Lambda(memberExpr, GraphQLSchema<TContext, TExecutionParameters>.DbParam, entityParam);

            // build args func wrapping selector expression, e.g. o => (db, p) => p.Id
            var objectParam = Expression.Parameter(typeof(object), "o");
            var argsExpr = Expression.Lambda(Expression.Quote(lambda), objectParam);
            var exprFunc = argsExpr.Compile();

            return GraphQLField.New(_schema, prop.Name.ToCamelCase(), (Func<object, LambdaExpression>) exprFunc, prop.PropertyType, _type);
        }

        public GraphQLFieldBuilder<TContext, TExecutionParameters, TField> AddPostField<TField>(string name, Func<TField> fieldFunc)
        {
            var field = GraphQLField.Post(_schema, name, fieldFunc);
            _type.OwnFields.Add(field);
            return new GraphQLFieldBuilder<TContext, TExecutionParameters, TField>(field);
        }
    }
}