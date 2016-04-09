using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using GraphQL.Parser;
using GraphQL.Parser.CS;

namespace GraphQL.Net.SchemaAdapters
{
    class SchemaType : SchemaQueryTypeCS<Info>
    {
        private readonly GraphQLType _type;
        private readonly Lazy<IReadOnlyDictionary<string, ISchemaField<Info>>> _fields;

        private SchemaType(GraphQLType type)
        {
            _type = type;
            _fields = new Lazy<IReadOnlyDictionary<string, ISchemaField<Info>>>(() => type.Fields
                .Select(f => new SchemaField(this, f))
                .ToDictionary(f => f.FieldName, f => f as ISchemaField<Info>));
        }

        public override IReadOnlyDictionary<string, ISchemaField<Info>> Fields => _fields.Value;
        public override string TypeName => _type.Name;
        public override string Description => _type.Description;
        public override Info Info => new Info(_type);

        // Statically cache one SchemaType per GraphQLType so we don't get into problems with recursive types.
        // Ideally this would be done at the schema root level instead of statically... just need to introduce references
        // to the root schema in all Schema* implementations.
        private static readonly Dictionary<GraphQLType, SchemaType> TypeMap = new Dictionary<GraphQLType, SchemaType>();

        public static SchemaType OfType(GraphQLType type)
        {
            SchemaType existing;
            if (TypeMap.TryGetValue(type, out existing)) return existing;
            return TypeMap[type] = new SchemaType(type);
        }
    }
}
