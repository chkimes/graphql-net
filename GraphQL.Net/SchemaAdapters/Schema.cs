using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using GraphQL.Parser;
using GraphQL.Parser.CS;

namespace GraphQL.Net.SchemaAdapters
{
    abstract class Schema : SchemaCS<Info>
    {
        internal readonly GraphQLSchema GraphQLSchema;
        protected Schema(GraphQLSchema schema)
        {
            GraphQLSchema = schema;
        }

        private readonly Dictionary<GraphQLType, SchemaType> _typeMap = new Dictionary<GraphQLType, SchemaType>();

        public SchemaType OfType(GraphQLType type)
        {
            SchemaType existing;
            if (_typeMap.TryGetValue(type, out existing)) return existing;
            return _typeMap[type] = new SchemaType(type, this);
        }
    }
    class Schema<TContext> : Schema
    {
        private readonly GraphQLSchema<TContext> _schema;
        private readonly Dictionary<string, ISchemaQueryType<Info>> _queryTypes;

        public Schema(GraphQLSchema<TContext> schema) : base(schema)
        {
            RootType = new SchemaRootType(this, schema.GetGQLType(typeof(TContext)));
            _schema = schema;
            _queryTypes = schema.Types
                .Where(t => !t.IsScalar)
                .Select(OfType)
                .ToDictionary(t => t.TypeName, t => t as ISchemaQueryType<Info>);
        }

        public override IReadOnlyDictionary<string, ISchemaQueryType<Info>> QueryTypes
            => _queryTypes;

        public override IReadOnlyDictionary<string, CoreVariableType> VariableTypes
            => _schema.VariableTypes.TypeDictionary;

        public override ISchemaQueryType<Info> RootType { get; }

        public override EnumValue ResolveEnumValue(string name)
        {
            return _schema.VariableTypes.ResolveEnumValue(name);
        }
    }
}