using System;
using System.Collections.Generic;

namespace GraphQL.Net
{
    public class GraphQL<TContext> where TContext : IDisposable, new()
    {
        public static GraphQLSchema<TContext> Schema = new GraphQLSchema<TContext>();

        private readonly GraphQLSchema<TContext> _schema;
        public GraphQL(GraphQLSchema<TContext> schema = null)
        {
            _schema = schema ?? Schema;
        }

        public static IDictionary<string, object> Execute(string query)
        {
            var gql = new GraphQL<TContext>();
            return gql.ExecuteQuery(query);
        }

        public IDictionary<string, object> ExecuteQuery(string queryStr)
        {
            var parsed = Parser.Parse(queryStr);
            var query = _schema.FindQuery(parsed.Name);
            return query.Execute(parsed);
        }
    }
}
