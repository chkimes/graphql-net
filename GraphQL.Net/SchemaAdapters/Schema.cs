using System.Collections.Generic;
using System.Linq;
using GraphQL.Parser;
using GraphQL.Parser.CS;

namespace GraphQL.Net.SchemaAdapters
{
    class Schema<TContext> : SchemaCS<Info>
    {
        private readonly Dictionary<string, ISchemaQueryType<Info>> _queryTypes;

        public Schema(GraphQLSchema<TContext> schema)
        {
            RootType = new SchemaRootType<TContext>(schema);
            _queryTypes = schema.Types
                .Where(t => !t.IsScalar)
                .Select(SchemaType.OfType)
                .ToDictionary(t => t.TypeName, t => t as ISchemaQueryType<Info>);
        }

        public override ISchemaQueryType<Info> ResolveQueryType(string name)
        {
            ISchemaQueryType<Info> result;
            return _queryTypes.TryGetValue(name, out result) ? result : null;
        }

        public override ISchemaQueryType<Info> RootType { get; }
    }
}