using GraphQL.Parser;
using GraphQL.Parser.CS;

namespace GraphQL.Net.SchemaAdapters
{
    class SchemaField : SchemaFieldCS<Info>
    {
        private readonly GraphQLField _field;

        public SchemaField(ISchemaQueryType<Info> declaringType, GraphQLField field)
        {
            DeclaringType = declaringType;
            _field = field;
            FieldType = _field.Type.IsScalar
                ? SchemaFieldType<Info>.NewValueField(VariableType.GuessFromCLRType(_field.Type.CLRType))
                : SchemaFieldType<Info>.NewQueryField(SchemaType.OfType(_field.Type));
        }

        public override ISchemaQueryType<Info> DeclaringType { get; }
        public override SchemaFieldType<Info> FieldType { get; }

        public override string FieldName => _field.Name;
        public override string Description => _field.Description;
        public override Info Info => new Info(_field);
    }
}