using System;
using GraphQL.Parser;

namespace GraphQL.Net
{
    public class GraphQLQueryBuilder
    {
        private readonly GraphQLQueryBase _query;

        internal GraphQLQueryBuilder(GraphQLQueryBase query)
        {
            _query = query;
        }

        public GraphQLQueryBuilder WithDescription(string description)
        {
            _query.Description = description;
            return this;
        }

        public GraphQLQueryBuilder WithComplexity(long min, long max)
        {
            _query.Complexity = Complexity.NewRange(Tuple.Create(min, max));
            return this;
        }
    }
}
