using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GraphQL.Net
{
    public static class SchemaExtensions
    {
        // This overload is provided to the user so they can shape TArgs with an anonymous type and rely on type inference for type parameters
        // e.g.  AddQuery("user", new { id = 0 }, (db, args) => db.Users.Where(u => u.Id == args.id));
        public static void AddQuery<TContext, TArgs, TEntity>(this GraphQLSchema<TContext> context, string name, TArgs argObj, Expression<Func<TContext, TArgs, TEntity>> queryableGetter)
            => AddQuery(context, name, queryableGetter);

        // Transform  (db, args) => db.Entities.Where(args)  into  args => db => db.Entities.Where(args)
        private static void AddQueryToListArgs<TContext, TArgs, TEntity>(this GraphQLSchema<TContext> context, string name, Expression<Func<TContext, TArgs, IQueryable<TEntity>>> queryableGetter)
        {
            var innerLambda = Expression.Lambda<Func<TContext, IQueryable<TEntity>>>(queryableGetter.Body, queryableGetter.Parameters[0]);
            context.AddQueryInternal(name, GetFinalQueryFunc<TContext, TArgs, IQueryable<TEntity>>(innerLambda, queryableGetter.Parameters[1]), ResolutionType.ToList);
        }

        // Transform  (db, args) => db.Entities.Where(args)  into  args => db => db.Entities.Where(args)
        public static void AddQuery<TContext, TArgs, TEntity>(this GraphQLSchema<TContext> context, string name, Expression<Func<TContext, TArgs, TEntity>> queryableGetter)
        {
            // TODO: Check for IQueryable
            var queryables = typeof (TEntity).GetInterfaces().Concat(new [] {typeof(TEntity)}).Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof (IQueryable<>)).ToList();
            if (queryables.Count > 1) throw new Exception("Types inheriting IQueryable<T> more than once are not supported.");
            if (queryables.Count == 1)
            {
                var entityType = queryables[0].GetGenericArguments()[0];
                var method = typeof (SchemaExtensions).GetMethod("AddQueryToListArgs", BindingFlags.Static | BindingFlags.NonPublic);
                var genMethod = method.MakeGenericMethod(typeof (TContext), typeof(TArgs), entityType);
                var getter = Expression.Lambda(typeof (Func<,,>).MakeGenericType(typeof (TContext), typeof(TArgs), queryables[0]), queryableGetter.Body, queryableGetter.Parameters[0], queryableGetter.Parameters[1]);
                genMethod.Invoke(null, new object[] {context, name, getter});
            }
            else
            {
                var innerLambda = Expression.Lambda<Func<TContext, TEntity>>(queryableGetter.Body, queryableGetter.Parameters[0]);
                var info = GetQueryInfo(innerLambda);
                if (info.ResolutionType != ResolutionType.Unmodified)
                    context.AddQueryInternal(name, GetFinalQueryFunc<TContext, TArgs, IQueryable<TEntity>>(info.BaseQuery, queryableGetter.Parameters[1]), info.ResolutionType);
                else
                    context.AddUnmodifiedQueryInternal(name, GetFinalQueryFunc<TContext, TArgs, TEntity>(info.OriginalQuery, queryableGetter.Parameters[1]));
            }
        }

        private static Func<TArgs, Expression<Func<TContext, TResult>>> GetFinalQueryFunc<TContext, TArgs, TResult>(Expression<Func<TContext, TResult>> baseExpr, ParameterExpression param = null)
        {
            // TODO: Replace db param here?
            param = param ?? Expression.Parameter(typeof (TArgs), "args");
            var quoted = Expression.Quote(baseExpr);
            var final = Expression.Lambda<Func<TArgs, Expression<Func<TContext, TResult>>>>(quoted, param);
            return final.Compile();
        }

        // Transform  db => db.Entities.Where(args)  into  args => db => db.Entities.Where(args)
        private static void AddQueryToListSimple<TContext, TEntity>(this GraphQLSchema<TContext> context, string name, Expression<Func<TContext, IQueryable<TEntity>>> queryableGetter)
        {
            context.AddQueryInternal(name, GetFinalQueryFunc<TContext, object, IQueryable<TEntity>>(queryableGetter), ResolutionType.ToList);
        }

        // Transform  db => db.Entities.Where(args)  into  args => db => db.Entities.Where(args)
        public static void AddQuery<TContext, TEntity>(this GraphQLSchema<TContext> context, string name, Expression<Func<TContext, TEntity>> queryableGetter)
        {
            var queryables = typeof (TEntity).GetInterfaces().Concat(new[] { typeof(TEntity) }).Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof (IQueryable<>)).ToList();
            if (queryables.Count > 1) throw new Exception("Types inheriting IQueryable<T> more than once are not supported.");
            if (queryables.Count == 1)
            {
                var entityType = queryables[0].GetGenericArguments()[0];
                var method = typeof (SchemaExtensions).GetMethod("AddQueryToListSimple", BindingFlags.Static | BindingFlags.NonPublic);
                var genMethod = method.MakeGenericMethod(typeof (TContext), entityType);
                var getter = Expression.Lambda(typeof (Func<,>).MakeGenericType(typeof (TContext), queryables[0]), queryableGetter.Body, queryableGetter.Parameters[0]);
                genMethod.Invoke(null, new object[] {context, name, getter});
            }
            else
            {
                var info = GetQueryInfo(queryableGetter);
                if (info.ResolutionType != ResolutionType.Unmodified)
                    context.AddQueryInternal(name, GetFinalQueryFunc<TContext, object, IQueryable<TEntity>>(info.BaseQuery), info.ResolutionType);
                else
                    context.AddUnmodifiedQueryInternal(name, GetFinalQueryFunc<TContext, object, TEntity>(info.OriginalQuery));
            }
        }

        private class QueryInfo<TContext, TEntity>
        {
            public Expression<Func<TContext, TEntity>> OriginalQuery;
            public Expression<Func<TContext, IQueryable<TEntity>>> BaseQuery;
            public ResolutionType ResolutionType;
        }

        private static QueryInfo<TContext, TEntity> GetQueryInfo<TContext, TEntity>(Expression<Func<TContext, TEntity>> queryableGetter)
        {
            var info = new QueryInfo<TContext, TEntity> {OriginalQuery = queryableGetter, ResolutionType = ResolutionType.Unmodified};
            var mce = queryableGetter.Body as MethodCallExpression;
            if (mce == null) return info;

            if (mce.Method.DeclaringType != typeof (Queryable)) return info; // TODO: Enumerable?
            if (!mce.Method.IsStatic) return info;
            if (mce.Method.Name != "First" && mce.Method.Name != "FirstOrDefault") return info;
            if (mce.Method.GetParameters().Length > 1) throw new Exception("First and FirstOrDefault are not supported with a predicate. Please use .Where(predicate).First() or .Where(predicate).FirstOrDefault().");

            switch (mce.Method.Name)
            {
                case "First":
                    info.ResolutionType = ResolutionType.First;
                    break;
                case "FirstOrDefault":
                    info.ResolutionType = ResolutionType.FirstOrDefault;
                    break;
                default:
                    throw new Exception("Can't get here");
            }

            var baseQueryable = mce.Arguments[0];
            info.BaseQuery = Expression.Lambda<Func<TContext, IQueryable<TEntity>>>(baseQueryable, queryableGetter.Parameters[0]);

            return info;
        }
    }
}