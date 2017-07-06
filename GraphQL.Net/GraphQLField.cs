using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using GraphQL.Net.SchemaAdapters;
using GraphQL.Parser;
using GraphQL.Parser.Execution;

namespace GraphQL.Net
{
    internal class GraphQLField
    {
        public string Name { get; protected set; }
        public string Description { get; set; }
        public bool IsList { get; protected set; }

        public bool IsPost { get; protected set; }
        public Func<object> PostFieldFunc { get; protected set; }

        public bool IsMutation { get; protected set; }

        internal Type FieldCLRType { get; set; }
        protected Type ArgsCLRType { get; set; }
        internal GraphQLType DefiningType { get; private set; }
        internal GraphQLSchema Schema { get; set; }

        // ExprFunc should be of type Func<TArgs, Expression<Func<TContext, TEntity, TField>>>
        // UNLESS Field is a mutation - then should be Func<TArgs, TMutReturn, Expresion<Func<TContext, TEntity, TField>>>
        protected Delegate ExprFunc { get; set; }

        // MutationFunc should be of type Action<TContext, TArgs>
        protected Delegate MutationFunc { get; set; }

        // lazily initialize type, fields may be defined before all types are loaded
        private GraphQLType _type;
        public GraphQLType Type => _type ?? (_type = Schema.GetGQLType(FieldCLRType));

        //TODO: Necessary for union types - find better solution
        internal void SetReturnType(GraphQLType type)
        {
            _type = type;
        }

        public virtual IEnumerable<ISchemaArgument<Info>> Arguments
            => TypeHelpers.GetArgs(Schema.VariableTypes, ArgsCLRType);

        public virtual LambdaExpression GetExpression(IEnumerable<ExecArgument<Info>> inputs, object mutationReturn = null)
            => IsMutation
            ? (LambdaExpression)ExprFunc.DynamicInvoke(TypeHelpers.GetArgs(ArgsCLRType, Schema.VariableTypes, inputs), mutationReturn)
            : (LambdaExpression)ExprFunc.DynamicInvoke(TypeHelpers.GetArgs(ArgsCLRType, Schema.VariableTypes, inputs));

        public virtual object RunMutation<TContext>(TContext context, IEnumerable<ExecArgument<Info>> inputs)
            => MutationFunc?.DynamicInvoke(context, TypeHelpers.GetArgs(ArgsCLRType, Schema.VariableTypes, inputs));

        public Complexity Complexity { get; set; }

        //TODO: Remove?
        public ResolutionType ResolutionType { get; set; }

        public static GraphQLField Post<TField>(GraphQLSchema schema, string name, Func<TField> fieldFunc)
        {
            return new GraphQLField
            {
                Schema = schema,
                Name = name,
                FieldCLRType = typeof(TField),
                ArgsCLRType = typeof(object),
                IsPost = true,
                PostFieldFunc = () => fieldFunc(),
            };
        }

        public static GraphQLField New<TArgs>(GraphQLSchema schema, string name, Func<TArgs, LambdaExpression> exprFunc, Type fieldCLRType, GraphQLType definingType)
            => NewInternal<TArgs>(schema, name, exprFunc, fieldCLRType, definingType, null);

        public static GraphQLField NewMutation<TContext, TArgs, TMutReturn>(GraphQLSchema schema, string name, Func<TArgs, TMutReturn, LambdaExpression> exprFunc, Type fieldCLRType, GraphQLType definingType, Func<TContext, TArgs, TMutReturn> mutationFunc)
            => NewInternal<TArgs>(schema, name, exprFunc, fieldCLRType, definingType, mutationFunc);

        private static GraphQLField NewInternal<TArgs>(GraphQLSchema schema, string name, Delegate exprFunc, Type fieldCLRType, GraphQLType definingType, Delegate mutationFunc)
        {
            var isList = false;
            if (fieldCLRType.IsGenericType && TypeHelpers.IsAssignableToGenericType(fieldCLRType, typeof(IEnumerable<>)))
            {
                fieldCLRType = fieldCLRType.GetGenericArguments()[0];
                isList = true;
            }

            return new GraphQLField
            {
                Schema = schema,
                Name = name,
                FieldCLRType = fieldCLRType,
                DefiningType = definingType,
                ArgsCLRType = typeof(TArgs),
                IsList = isList,
                ExprFunc = exprFunc,
                MutationFunc = mutationFunc,
                IsMutation = mutationFunc != null,
            };
        }
    }
}