using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace GraphQL.Net
{
    internal abstract class GraphQLField
    {
        public string Name { get; protected set; }
        public string Description { get; set; }
        public virtual bool IsList => false;

        public abstract GraphQLType Type { get; }
        public abstract LambdaExpression GetExpression(List<Input> inputs);
    }

    internal class GraphQLField<TContext, TArgs, TEntity, TField> : GraphQLField
    {
        protected readonly GraphQLSchema<TContext> Schema;
        protected readonly Func<TArgs, Expression<Func<TContext, TEntity, TField>>> ExprFunc;

        public GraphQLField(GraphQLSchema<TContext> schema, string name, Func<TArgs, Expression<Func<TContext, TEntity, TField>>> exprFunc)
        {
            Schema = schema;
            ExprFunc = exprFunc;
            Name = name;
        }

        public override LambdaExpression GetExpression(List<Input> inputs)
        {
            var args = TypeHelpers.GetArgs<TArgs>(inputs);
            return ExprFunc(args);
        }

        // lazily initialize type, fields may be defined before all types are loaded
        private GraphQLType _type;
        public override GraphQLType Type => _type ?? (_type = Schema.GetGQLType(typeof(TField)));
    }

    internal class GraphQLListField<TContext, TArgs, TEntity, TField> : GraphQLField<TContext, TArgs, TEntity, List<TField>>
    {
        public GraphQLListField(GraphQLSchema<TContext> schema, string name, Func<TArgs, Expression<Func<TContext, TEntity, List<TField>>>> exprFunc)
            : base(schema, name, exprFunc) { }

        public override bool IsList => true;

        // this has to be copied since we're using the type TField and not List<TField> from the base class
        private GraphQLType _type;
        public override GraphQLType Type => _type ?? (_type = Schema.GetGQLType(typeof(TField)));
    }
}