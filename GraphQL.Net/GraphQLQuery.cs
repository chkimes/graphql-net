using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace GraphQL.Net
{
    internal abstract class GraphQLQueryBase
    {
        public string Name { get; set; }
        public GraphQLType Type { get; set; }
        public ResolutionType ResolutionType { get; set; }
        public abstract IDictionary<string, object> Execute(Query query);
    }

    internal abstract class GraphQLQueryBase<TContext> : GraphQLQueryBase
    {
        public GraphQLSchema<TContext> Schema { get; set; }
        public abstract IDictionary<string, object> Execute(TContext context, Query query);
        public Func<TContext> ContextCreator { get; set; }
    }

    internal class GraphQLQuery<TContext, TArgs, TEntity> : GraphQLQueryBase<TContext>
    {
        public Func<TArgs, Expression<Func<TContext, TEntity>>> ExprGetter { get; set; }
        public Func<TArgs, Expression<Func<TContext, IQueryable<TEntity>>>> QueryableExprGetter { get; set; }

        public override IDictionary<string, object> Execute(Query query)
        {
            // Goofy hack to provide Executor with all the type information it needs
            return Executor<TContext>.Execute(Schema, this, query);
        }

        public override IDictionary<string, object> Execute(TContext context, Query query)
        {
            return Executor<TContext>.Execute(context, this, query);
        }
    }

    internal enum ResolutionType
    {
        Unmodified,
        ToList,
        FirstOrDefault,
        First
    }
}
