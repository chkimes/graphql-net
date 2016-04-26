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
        // e.g.  AddQuery("user", new { id = 0 }, (db, args) => db.Users.Where(u => u.Id == args.id));
        public static GraphQLFieldBuilder<TContext, TEntity> AddQuery<TContext, TArgs, TEntity>(this GraphQLSchema<TContext> context, string name, TArgs argObj, Expression<Func<TContext, TArgs, TEntity>> queryableGetter)
            => AddQuery(context, name, queryableGetter);

        public static GraphQLFieldBuilder<TContext, TEntity> AddListQuery<TContext, TArgs, TEntity>(this GraphQLSchema<TContext> context, string name, TArgs argObj, Expression<Func<TContext, TArgs, IEnumerable<TEntity>>> queryableGetter)
            => AddListQuery(context, name, queryableGetter);

        // Transform  (db, args) => db.Entities.Where(args)  into  args => db => db.Entities.Where(args)
        private static GraphQLFieldBuilder<TContext, TEntity> AddListQuery<TContext, TArgs, TEntity>(this GraphQLSchema<TContext> context, string name, Expression<Func<TContext, TArgs, IEnumerable<TEntity>>> queryableGetter)
        {
            var innerLambda = Expression.Lambda<Func<TContext, IEnumerable<TEntity>>>(queryableGetter.Body, queryableGetter.Parameters[0]);
            return context.AddQueryInternal(name, GetFinalQueryFunc<TContext, TArgs, IEnumerable<TEntity>>(innerLambda, queryableGetter.Parameters[1]), ResolutionType.ToList);
        }

        // Transform  (db, args) => db.Entities.Where(args)  into  args => db => db.Entities.Where(args)
        public static GraphQLFieldBuilder<TContext, TEntity> AddQuery<TContext, TArgs, TEntity>(this GraphQLSchema<TContext> context, string name, Expression<Func<TContext, TArgs, TEntity>> queryableGetter)
        {
            var queryables = typeof (TEntity).GetInterfaces().Concat(new [] {typeof(TEntity)}).Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof (IQueryable<>)).ToList();
            if (queryables.Count > 1) throw new Exception("Types inheriting IQueryable<T> more than once are not supported.");
            if (queryables.Count == 1)
            {
                throw new Exception("use the other one");
                var entityType = queryables[0].GetGenericArguments()[0];
                var method = typeof (SchemaExtensions).GetMethod("AddQueryToListArgs", BindingFlags.Static | BindingFlags.NonPublic);
                var genMethod = method.MakeGenericMethod(typeof (TContext), typeof(TArgs), entityType);
                var getter = Expression.Lambda(typeof (Func<,,>).MakeGenericType(typeof (TContext), typeof(TArgs), queryables[0]), queryableGetter.Body, queryableGetter.Parameters[0], queryableGetter.Parameters[1]);
                return (GraphQLFieldBuilder<TContext, TEntity>)genMethod.Invoke(null, new object[] {context, name, getter});
            }
            else
            {
                var innerLambda = Expression.Lambda<Func<TContext, TEntity>>(queryableGetter.Body, queryableGetter.Parameters[0]);
                var info = GetQueryInfo(innerLambda);
                if (info.ResolutionType != ResolutionType.Unmodified)
                    return context.AddQueryInternal(name, GetFinalQueryFunc<TContext, TArgs, IEnumerable<TEntity>>(info.BaseQuery, queryableGetter.Parameters[1]), info.ResolutionType);
                else
                    return context.AddUnmodifiedQueryInternal(name, GetFinalQueryFunc<TContext, TArgs, TEntity>(info.OriginalQuery, queryableGetter.Parameters[1]));
            }
        }

        private static Func<TArgs, Expression<Func<TContext, BaseQuery, TResult>>> GetFinalQueryFunc<TContext, TArgs, TResult>(Expression<Func<TContext, TResult>> baseExpr, ParameterExpression param = null)
        {
            // TODO: Replace db param here?
            param = param ?? Expression.Parameter(typeof (TArgs), "args");
            var transformedExpr = Expression.Lambda(Expression.Convert(baseExpr.Body, typeof(TResult)), baseExpr.Parameters[0], Expression.Parameter(typeof (BaseQuery), "base"));
            var quoted = Expression.Quote(transformedExpr);
            var final = Expression.Lambda<Func<TArgs, Expression<Func<TContext, BaseQuery, TResult>>>>(quoted, param);
            return final.Compile();
        }

        // Transform  db => db.Entities.Where(args)  into  args => db => db.Entities.Where(args)
        public static GraphQLFieldBuilder<TContext, TEntity> AddListQuery<TContext, TEntity>(this GraphQLSchema<TContext> context, string name, Expression<Func<TContext, IEnumerable<TEntity>>> queryableGetter)
        {
            return context.AddQueryInternal(name, GetFinalQueryFunc<TContext, object, IEnumerable<TEntity>>(queryableGetter), ResolutionType.ToList);
        }

        // Transform  db => db.Entities.Where(args)  into  args => db => db.Entities.Where(args)
        public static GraphQLFieldBuilder<TContext, TEntity> AddQuery<TContext, TEntity>(this GraphQLSchema<TContext> context, string name, Expression<Func<TContext, TEntity>> queryableGetter)
        {
            var enumerables = typeof (TEntity).GetInterfaces().Concat(new[] { typeof(TEntity) }).Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof (IEnumerable<>)).ToList();
            if (enumerables.Count > 1) throw new Exception("Types inheriting IQueryable<T> more than once are not supported.");
            if (enumerables.Count == 1)
            {
                throw new Exception("use the other one");
                var entityType = enumerables[0].GetGenericArguments()[0];
                var method = typeof (SchemaExtensions).GetMethod("AddQueryToListSimple", BindingFlags.Static | BindingFlags.NonPublic);
                var genMethod = method.MakeGenericMethod(typeof (TContext), entityType);
                var enumerableType = typeof (IEnumerable<>).MakeGenericType(entityType);
                var getter = Expression.Lambda(typeof (Func<,>).MakeGenericType(typeof (TContext), enumerables[0]), Expression.Convert(queryableGetter.Body, enumerableType), queryableGetter.Parameters[0]);
                var obj = genMethod.Invoke(null, new object[] {context, name, getter});
                return (GraphQLFieldBuilder<TContext, TEntity>) obj;
            }
            else
            {
                var info = GetQueryInfo(queryableGetter);
                if (info.ResolutionType != ResolutionType.Unmodified)
                    return context.AddQueryInternal(name, GetFinalQueryFunc<TContext, object, IEnumerable<TEntity>>(info.BaseQuery), info.ResolutionType);
                else
                    return context.AddUnmodifiedQueryInternal(name, GetFinalQueryFunc<TContext, object, TEntity>(info.OriginalQuery));
            }
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