using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphQL.Net
{
    public class GraphQLQueryBuilder<TArgs>
    {
        private readonly GraphQLQueryBase _query;

        private GraphQLQueryBuilder(GraphQLQueryBase query)
        {
            _query = query;
        } 

        internal static GraphQLQueryBuilder<TArgs> New<TContext, TEntity>(GraphQLQuery<TContext, TArgs, TEntity> query)
            => new GraphQLQueryBuilder<TArgs>(query);

        public GraphQLQueryBuilder<TArgs> WithDescription(string description)
        {
            _query.Description = description;
            return this;
        }
    }
}
