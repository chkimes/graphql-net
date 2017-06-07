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
        private readonly Lazy<IEnumerable<ISchemaQueryType<Info>>> _possibleTypes;

        internal SchemaType(GraphQLType type, Schema schema)
        {
            _type = type;
            _fields = new Lazy<IReadOnlyDictionary<string, ISchemaField<Info>>>(() => type.GetAllFieldIncludeBaseType()
                .Select(f => new SchemaField(this, f, schema))
                // There might be duplicates (i.e. '__typename' on types with a base type) - ignore them.
                .Aggregate(
                    new Dictionary<string, ISchemaField<Info>>(),
                    (dict, field) =>
                    {
                        dict[field.FieldName] = field;
                        return dict;
                    }
                    ));
            
            _possibleTypes = new Lazy<IEnumerable<ISchemaQueryType<Info>>>(
                () => type.IncludedTypes.Select(schema.OfType)
                    // Add possible types recursively
                    .SelectMany(t => new List<ISchemaQueryType<Info>> { t }.Concat(t.PossibleTypes)));
        }

        public override IReadOnlyDictionary<string, ISchemaField<Info>> Fields => _fields.Value;
        public override string TypeName => _type.Name;
        public override string Description => _type.Description;
        public override Info Info => new Info(_type);
        public override IEnumerable<ISchemaQueryType<Info>> PossibleTypes => _possibleTypes.Value;
    }
}
