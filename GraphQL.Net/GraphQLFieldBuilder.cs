using System;
using GraphQL.Parser;

namespace GraphQL.Net
{
    public class GraphQLFieldBuilder<TContext, TField>
    {
        private readonly GraphQLField _field;

        internal GraphQLFieldBuilder(GraphQLField field)
        {
            _field = field;
        }

        public GraphQLFieldBuilder<TContext, TField> WithDescription(string description)
        {
            _field.Description = description;
            return this;
        }

        public GraphQLFieldBuilder<TContext, TField> WithComplexity(long min, long max)
        {
            _field.Complexity = Complexity.NewRange(Tuple.Create(min, max));
            return this;
        }

        public GraphQLFieldBuilder<TContext, TField> WithReturnType(IGraphQLType type)
        {
            _field.SetReturnType(type as GraphQLType);
            return this;
        }

        // TODO: This should be removed once we figure out a better way to do it
        internal GraphQLFieldBuilder<TContext, TField> WithResolutionType(ResolutionType type)
        {
            _field.ResolutionType = type;
            return this;
        }
    }
}