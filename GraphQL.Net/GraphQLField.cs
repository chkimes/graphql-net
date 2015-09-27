using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace GraphQL.Net
{
    internal abstract class GraphQLField
    {
        public string Name { get; protected set; }
        public string Description { get; set; }

        public abstract GraphQLType Type { get; }
        public abstract LambdaExpression GetExpression(List<Input> inputs);
    }

    internal class GraphQLField<TContext, TArgs, TEntity, TField> : GraphQLField
    {
        private readonly GraphQLSchema<TContext> _schema;
        private readonly Func<TArgs, Expression<Func<TContext, TEntity, TField>>> _exprFunc;

        public GraphQLField(GraphQLSchema<TContext> schema, string name, Func<TArgs, Expression<Func<TContext, TEntity, TField>>> exprFunc)
        {
            _schema = schema;
            _exprFunc = exprFunc;

            Name = name;
        }

        public override LambdaExpression GetExpression(List<Input> inputs)
        {
            var args = TypeHelpers.GetArgs<TArgs>(inputs);
            return _exprFunc(args);
        }

        // lazily initialize type, fields may be defined before all types are loaded
        private GraphQLType _type;
        public override GraphQLType Type => _type ?? (_type = _schema.GetGQLType(typeof(TField)));
    }
}