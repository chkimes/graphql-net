using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using GraphQL.Net.SchemaAdapters;
using GraphQL.Parser;
using GraphQL.Parser.Execution;

namespace GraphQL.Net
{
    internal abstract class GraphQLQueryBase
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public GraphQLType Type { get; set; }
        public ResolutionType ResolutionType { get; set; }
        public abstract object Execute(ExecSelection<Info> query);
    }

    internal abstract class GraphQLQueryBase<TContext> : GraphQLQueryBase
    {
        public GraphQLSchema<TContext> Schema { get; set; }
        public abstract IEnumerable<ISchemaArgument<Info>> Arguments { get; }
        public abstract object Execute(TContext context, ExecSelection<Info> query);
    }

    internal class GraphQLQuery<TContext, TArgs, TEntity> : GraphQLQueryBase<TContext>
    {
        public Func<TArgs, Expression<Func<TContext, TEntity>>> ExprGetter { get; set; }
        public Func<TArgs, Expression<Func<TContext, IQueryable<TEntity>>>> QueryableExprGetter { get; set; }

        public override IEnumerable<ISchemaArgument<Info>> Arguments => TypeHelpers.GetArgs<TArgs>(Schema.VariableTypes);

        public override object Execute(ExecSelection<Info> query)
        {
            // Goofy hack to provide Executor with all the type information it needs
            return Executor<TContext>.Execute(Schema, this, query);
        }

        public override object Execute(TContext context, ExecSelection<Info> query)
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
