using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace GraphQL.Net
{
    internal abstract class GraphQLField
    {
        protected GraphQLField(Type fieldType)
        {
            FieldType = fieldType;
        }

        public string Name { get; set; }
        public string Description { get; set; }

        public Type FieldType { get; set; }
        public abstract GraphQLType Type { get; }

        public abstract LambdaExpression GetExpression(List<Input> inputs);
    }

    internal abstract class GraphQLField<TContext> : GraphQLField
    {
        private readonly GraphQLSchema<TContext> _schema;

        protected GraphQLField(GraphQLSchema<TContext> schema, Type fieldType) : base(fieldType)
        {
            _schema = schema;
        }

        // lazily initialize type, fields may be defined before all types are loaded
        private GraphQLType _type;
        public override GraphQLType Type => _type ?? (_type = _schema.GetGQLType(FieldType));
    }

    internal class GraphQLField<TContext, TArgs, TEntity, TField> : GraphQLField<TContext>
    {
        public GraphQLField(GraphQLSchema<TContext> schema) : base(schema, typeof(TField)) { }

        public Func<TArgs, Expression<Func<TContext, TEntity, TField>>> ExprFunc;

        public override LambdaExpression GetExpression(List<Input> inputs)
        {
            var args = TypeHelpers.GetArgs<TArgs>(inputs);
            return ExprFunc(args);
        }
    }
}