using System;
using GraphQL.Parser;

namespace GraphQL.Net
{
    public class GraphQLFieldBuilder
    {
        private readonly GraphQLField _field;

        internal GraphQLFieldBuilder(GraphQLField field)
        {
            _field = field;
        }

        public GraphQLFieldBuilder WithDescription(string description)
        {
            _field.Description = description;
            return this;
        }

        public GraphQLFieldBuilder WithComplexity(long min, long max)
        {
            _field.Complexity = Complexity.NewRange(Tuple.Create(min, max));
            return this;
        }
    }
}