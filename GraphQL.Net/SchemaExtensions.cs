using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GraphQL.Net
{
    public static class SchemaExtensions
    {
        // This overload is provided to the user so they can shape TArgs with an anonymous type and rely on type inference for type parameters
        // e.g.  AddField("user", new { id = 0 }, (db, args) => db.Users.Where(u => u.Id == args.id));
        public static GraphQLFieldBuilder<TContext, TEntity> AddField<TContext, TArgs, TEntity>(this GraphQLSchema<TContext> context, string name, TArgs argObj, Expression<Func<TContext, TArgs, TEntity>> queryableGetter)
            => AddField(context, name, queryableGetter);

        public static GraphQLFieldBuilder<TContext, TEntity> AddListField<TContext, TArgs, TEntity>(this GraphQLSchema<TContext> context, string name, TArgs argObj, Expression<Func<TContext, TArgs, IEnumerable<TEntity>>> queryableGetter)
            => AddListField(context, name, queryableGetter);

        public static GraphQLFieldBuilder<TContext, TEntity> AddMutation<TContext, TArgs, TEntity, TMutReturn>(this GraphQLSchema<TContext> context, string name, TArgs argObj, Func<TContext, TArgs, TMutReturn> mutation, Expression<Func<TContext, TArgs, TMutReturn, TEntity>> queryableGetter)
            => AddMutation(context, name, queryableGetter, mutation);

        public static GraphQLFieldBuilder<TContext, TEntity> AddListMutation<TContext, TArgs, TEntity, TMutReturn>(this GraphQLSchema<TContext> context, string name, TArgs argObj, Func<TContext, TArgs, TMutReturn> mutation, Expression<Func<TContext, TArgs, TMutReturn, IEnumerable<TEntity>>> queryableGetter)
            => AddListMutation(context, name, queryableGetter, mutation);

        public static GraphQLFieldBuilder<TContext, TEntity> AddMutation<TContext, TArgs, TEntity>(this GraphQLSchema<TContext> context, string name, TArgs argObj, Action<TContext, TArgs> mutation, Expression<Func<TContext, TArgs, TEntity>> queryableGetter)
        {
            var transformed = Expression.Lambda<Func<TContext, TArgs, object, TEntity>>(queryableGetter.Body, queryableGetter.Parameters[0], queryableGetter.Parameters[1], Expression.Parameter(typeof(object), "mutRet"));
            return AddMutation(context, name, transformed, (db, args) => { mutation(db, args); return null; });
        }

        public static GraphQLFieldBuilder<TContext, TEntity> AddListMutation<TContext, TArgs, TEntity>(this GraphQLSchema<TContext> context, string name, TArgs argObj, Action<TContext, TArgs> mutation, Expression<Func<TContext, TArgs, IEnumerable<TEntity>>> queryableGetter)
        {
            var transformed = Expression.Lambda<Func<TContext, TArgs, object, IEnumerable<TEntity>>>(queryableGetter.Body, queryableGetter.Parameters[0], queryableGetter.Parameters[1], Expression.Parameter(typeof(object), "mutRet"));
            return AddListMutation(context, name, transformed, (db, args) => { mutation(db, args); return null; });
        }

        // Transform  (db, args) => db.Entities.Where(args)  into  args => db => db.Entities.Where(args)
        private static GraphQLFieldBuilder<TContext, TEntity> AddListField<TContext, TArgs, TEntity>(this GraphQLSchema<TContext> context, string name, Expression<Func<TContext, TArgs, IEnumerable<TEntity>>> queryableGetter)
        {
            var innerLambda = Expression.Lambda<Func<TContext, IEnumerable<TEntity>>>(queryableGetter.Body, queryableGetter.Parameters[0]);
            return context.AddFieldInternal(name, GetFinalQueryFunc<TContext, TArgs, IEnumerable<TEntity>>(innerLambda, queryableGetter.Parameters[1]), ResolutionType.ToList);
        }

        // Transform  (db, args) => db.Entities.First(args)  into  args => db => db.Entities.First(args)
        private static GraphQLFieldBuilder<TContext, TEntity> AddField<TContext, TArgs, TEntity>(this GraphQLSchema<TContext> context, string name, Expression<Func<TContext, TArgs, TEntity>> queryableGetter)
        {
            var innerLambda = Expression.Lambda<Func<TContext, TEntity>>(queryableGetter.Body, queryableGetter.Parameters[0]);
            var info = GetQueryInfo(innerLambda);
            if (info.ResolutionType != ResolutionType.Unmodified)
                return context.AddFieldInternal(name, GetFinalQueryFunc<TContext, TArgs, IEnumerable<TEntity>>(info.BaseQuery, queryableGetter.Parameters[1]), info.ResolutionType);
            return context.AddUnmodifiedFieldInternal(name, GetFinalQueryFunc<TContext, TArgs, TEntity>(info.OriginalQuery, queryableGetter.Parameters[1]));
        }

        // Transform  (db, args, mut) => db.Entities.Where(args)  into  args => db => db.Entities.Where(args)
        private static GraphQLFieldBuilder<TContext, TEntity> AddListMutation<TContext, TArgs, TEntity, TMutReturn>(this GraphQLSchema<TContext> context, string name, Expression<Func<TContext, TArgs, TMutReturn, IEnumerable<TEntity>>> queryableGetter, Func<TContext, TArgs, TMutReturn> mutation)
        {
            var innerLambda = Expression.Lambda<Func<TContext, IEnumerable<TEntity>>>(queryableGetter.Body, queryableGetter.Parameters[0]);
            return context.AddMutationInternal(name, GetFinalMutationFunc<TContext, TArgs, TMutReturn, IEnumerable<TEntity>>(innerLambda, queryableGetter.Parameters[1], queryableGetter.Parameters[2]), ResolutionType.ToList, mutation);
        }

        // Transform  (db, args, mut) => db.Entities.First(args)  into  args => db => db.Entities.First(args)
        private static GraphQLFieldBuilder<TContext, TEntity> AddMutation<TContext, TArgs, TEntity, TMutReturn>(this GraphQLSchema<TContext> context, string name, Expression<Func<TContext, TArgs, TMutReturn, TEntity>> queryableGetter, Func<TContext, TArgs, TMutReturn> mutation)
        {
            var innerLambda = Expression.Lambda<Func<TContext, TEntity>>(queryableGetter.Body, queryableGetter.Parameters[0]);
            var info = GetQueryInfo(innerLambda);
            if (info.ResolutionType != ResolutionType.Unmodified)
                return context.AddMutationInternal(name, GetFinalMutationFunc<TContext, TArgs, TMutReturn, IEnumerable<TEntity>>(info.BaseQuery, queryableGetter.Parameters[1], queryableGetter.Parameters[2]), info.ResolutionType, mutation);
            return context.AddUnmodifiedMutationInternal(name, GetFinalMutationFunc<TContext, TArgs, TMutReturn, TEntity>(info.OriginalQuery, queryableGetter.Parameters[1], queryableGetter.Parameters[2]), mutation);
        }

        private static Func<TArgs, Expression<Func<TContext, TContext, TResult>>> GetFinalQueryFunc<TContext, TArgs, TResult>(Expression<Func<TContext, TResult>> baseExpr, ParameterExpression param = null)
        {
            // TODO: Replace db param here?
            param = param ?? Expression.Parameter(typeof (TArgs), "args");
            var transformedExpr = Expression.Lambda(Expression.Convert(baseExpr.Body, typeof(TResult)), baseExpr.Parameters[0], Expression.Parameter(typeof (TContext), "base"));
            var quoted = Expression.Quote(transformedExpr);
            var final = Expression.Lambda<Func<TArgs, Expression<Func<TContext, TContext, TResult>>>>(quoted, param);
            return final.Compile();
        }

        private static Func<TArgs, TMutReturn, Expression<Func<TContext, TContext, TResult>>> GetFinalMutationFunc<TContext, TArgs, TMutReturn, TResult>(Expression<Func<TContext, TResult>> baseExpr, ParameterExpression argParam, ParameterExpression retParam)
        {
            // TODO: Replace db param here?
            argParam = argParam ?? Expression.Parameter(typeof (TArgs), "args");
            var transformedExpr = Expression.Lambda(Expression.Convert(baseExpr.Body, typeof(TResult)), baseExpr.Parameters[0], Expression.Parameter(typeof (TContext), "base"));
            var quoted = Expression.Quote(transformedExpr);
            var final = Expression.Lambda<Func<TArgs, TMutReturn, Expression<Func<TContext, TContext, TResult>>>>(quoted, argParam, retParam);
            return final.Compile();
        }

        // Transform  db => db.Entities.Where(args)  into  args => db => db.Entities.Where(args)
        public static GraphQLFieldBuilder<TContext, TEntity> AddListField<TContext, TEntity>(this GraphQLSchema<TContext> context, string name, Expression<Func<TContext, IEnumerable<TEntity>>> queryableGetter)
        {
            return context.AddFieldInternal(name, GetFinalQueryFunc<TContext, object, IEnumerable<TEntity>>(queryableGetter), ResolutionType.ToList);
        }

        // Transform  db => db.Entities.Where(args)  into  args => db => db.Entities.Where(args)
        public static GraphQLFieldBuilder<TContext, TEntity> AddField<TContext, TEntity>(this GraphQLSchema<TContext> context, string name, Expression<Func<TContext, TEntity>> queryableGetter)
        {
            var info = GetQueryInfo(queryableGetter);
            if (info.ResolutionType != ResolutionType.Unmodified)
                return context.AddFieldInternal(name, GetFinalQueryFunc<TContext, object, IEnumerable<TEntity>>(info.BaseQuery), info.ResolutionType);
            return context.AddUnmodifiedFieldInternal(name, GetFinalQueryFunc<TContext, object, TEntity>(info.OriginalQuery));
        }

        private class QueryInfo<TContext, TEntity>
        {
            public Expression<Func<TContext, TEntity>> OriginalQuery;
            public Expression<Func<TContext, IEnumerable<TEntity>>> BaseQuery;
            public ResolutionType ResolutionType;
        }

        private static QueryInfo<TContext, TEntity> GetQueryInfo<TContext, TEntity>(Expression<Func<TContext, TEntity>> queryableGetter)
        {
            var info = new QueryInfo<TContext, TEntity> {OriginalQuery = queryableGetter, ResolutionType = ResolutionType.Unmodified};
            var mce = queryableGetter.Body as MethodCallExpression;
            if (mce == null) return info;

            if (mce.Method.DeclaringType != typeof (Queryable)) return info; // TODO: Enumerable?
            if (!mce.Method.IsStatic) return info;

            switch (mce.Method.Name)
            {
                case "First":
                    info.ResolutionType = ResolutionType.First;
                    break;
                case "FirstOrDefault":
                    info.ResolutionType = ResolutionType.FirstOrDefault;
                    break;
                default:
                    return info;
            }

            var baseQueryable = mce.Arguments[0];
            if (mce.Arguments.Count > 1)
            {
                baseQueryable = Expression.Call(typeof (Queryable), "Where", new[] {typeof (TEntity)}, baseQueryable, mce.Arguments[1]);
            }

            info.BaseQuery = Expression.Lambda<Func<TContext, IEnumerable<TEntity>>>(baseQueryable, queryableGetter.Parameters[0]);

            return info;
        }
    }
}