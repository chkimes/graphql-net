using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GraphQL.Parser;
using GraphQL.Parser.CS;

namespace GraphQL.Net.SchemaAdapters
{
    class SchemaMutationType : SchemaQueryTypeCS<Info>
    {
        public SchemaMutationType(Schema schema, GraphQLType baseQueryType)
        {
            Fields = baseQueryType.Fields
                .Where(f => f.IsMutation)
                .Select(f => new SchemaField(this, f, schema))
                .ToDictionary(f => f.FieldName, f => f as ISchemaField<Info>);
        }

        public override IReadOnlyDictionary<string, ISchemaField<Info>> Fields { get; }
        public override string TypeName => "queryType";

        public override IEnumerable<ISchemaQueryType<Info>> PossibleTypes => new Collection<ISchemaQueryType<Info>>();
        public override IEnumerable<ISchemaQueryType<Info>> Interfaces => new Collection<ISchemaQueryType<Info>>();
    }
}