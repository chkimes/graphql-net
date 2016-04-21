using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Parser;
using GraphQL.Parser.CS;

namespace GraphQL.Net.SchemaAdapters
{
    class SchemaField : SchemaFieldCS<Info>
    {
        private readonly Schema _schema;
        private readonly GraphQLField _field;

        public SchemaField(ISchemaQueryType<Info> declaringType, GraphQLField field, Schema schema)
        {
            DeclaringType = declaringType;
            _field = field;
            _schema = schema;
            if (_field.Type.IsScalar)
            {
                var varType = _schema.GraphQLSchema.VariableTypes.VariableTypeOf(_field.Type.CLRType);
                FieldType = SchemaFieldType<Info>.NewValueField(varType);
            }
            else
            {
                FieldType = SchemaFieldType<Info>.NewQueryField(_schema.OfType(_field.Type));;
            }
            Arguments = _field.Arguments.ToDictionary(a => a.ArgumentName);
        }

        public override ISchemaQueryType<Info> DeclaringType { get; }
        public override SchemaFieldType<Info> FieldType { get; }

        public override string FieldName => _field.Name;
        public override string Description => _field.Description;
        public override Info Info => new Info(_field);
        public override IReadOnlyDictionary<string, ISchemaArgument<Info>> Arguments { get; }
        public override Complexity EstimateComplexity(IEnumerable<ISchemaArgument<Info>> args)
        {
            if (_field.Type.IsScalar) return Complexity.Zero; // scalars are practically free to select
            if (!_field.IsList) return Complexity.One;
            return args.Any(a => a.ArgumentName.Equals("id", StringComparison.OrdinalIgnoreCase))
                ? Complexity.One
                // wild guess: we can have 0 to 200 related entities
                // can we let the underlying schema provide more accurate info here?
                : Complexity.Of(0, 200);
        }
    }
}