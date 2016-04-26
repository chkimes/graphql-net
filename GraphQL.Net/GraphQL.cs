using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Net.SchemaAdapters;
using GraphQL.Parser;
using GraphQL.Parser.Execution;

namespace GraphQL.Net
{
    public class GraphQL<TContext>
    {
        public static GraphQLSchema<TContext> Schema;

        private readonly GraphQLSchema<TContext> _schema;
        public GraphQL(GraphQLSchema<TContext> schema = null)
        {
            _schema = schema ?? Schema;
        }

        public static GraphQLSchema<TContext> CreateDefaultSchema(Func<TContext> creationFunc)
        {
            return Schema = new GraphQLSchema<TContext>(creationFunc);
        }

        public static IDictionary<string, object> Execute(string query)
        {
            var gql = new GraphQL<TContext>();
            return gql.ExecuteQuery(query);
        }

        public IDictionary<string, object> ExecuteQuery(string queryStr)
        {
            if (!_schema.Completed)
                throw new InvalidOperationException("Schema must be Completed before executing a query. Try calling the schema's Complete method.");

            var document = GraphQLDocument<Info>.Parse(_schema.Adapter, queryStr);
            var context = DefaultExecContext.Instance; // TODO use a real IExecContext to support passing variables
            var operation = document.Operations.Single(); // TODO support multiple operations per document, look up by name
            var execSelections = context.ToExecSelections(operation.Value);
            var outputs = new Dictionary<string, object>();
            foreach (var execSelection in execSelections.Select(s => s.Value))
            {
                var field = execSelection.SchemaField.Field();
                outputs[execSelection.Name] = Executor<TContext>.Execute(_schema, field, execSelection);
            }
            return outputs;
        }
    }
}
