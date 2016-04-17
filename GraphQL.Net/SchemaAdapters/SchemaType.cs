using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Parser;
using GraphQL.Parser.CS;

namespace GraphQL.Net.SchemaAdapters
{
    class SchemaType : SchemaQueryTypeCS<Info>
    {
        private readonly GraphQLType _type;
        private readonly Lazy<IReadOnlyDictionary<string, ISchemaField<Info>>> _fields;

        internal SchemaType(GraphQLType type, Schema schema)
        {
            _type = type;
            _fields = new Lazy<IReadOnlyDictionary<string, ISchemaField<Info>>>(() => type.Fields
                .Select(f => new SchemaField(this, f, schema))
                .ToDictionary(f => f.FieldName, f => f as ISchemaField<Info>));
        }

        public override IReadOnlyDictionary<string, ISchemaField<Info>> Fields => _fields.Value;
        public override string TypeName => _type.Name;
        public override string Description => _type.Description;
        public override Info Info => new Info(_type);
    }
}
