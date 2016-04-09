using System.Collections.Generic;
using System.Linq;
using GraphQL.Parser;
using GraphQL.Parser.CS;

namespace GraphQL.Net.SchemaAdapters
{
    class SchemaRootType<TContext> : SchemaQueryTypeCS<Info>
    {
        public SchemaRootType(GraphQLSchema<TContext> schema)
        {
            Fields = schema.Queries
                .Select(f => new SchemaQueryField<TContext>(this, f))
                .ToDictionary(f => f.FieldName, f => f as ISchemaField<Info>);
        }

        public override IReadOnlyDictionary<string, ISchemaField<Info>> Fields { get; }
        public override string TypeName => "SchemaRoot";
    }
}
