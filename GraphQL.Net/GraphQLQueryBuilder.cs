using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
