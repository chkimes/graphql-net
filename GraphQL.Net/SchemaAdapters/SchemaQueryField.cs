using GraphQL.Parser;
using GraphQL.Parser.CS;

namespace GraphQL.Net.SchemaAdapters
{
    class SchemaQueryField<TContext> : SchemaQueryableFieldCS<Info>
    {
        private readonly SchemaRootType<TContext> _declaringType;
        private readonly GraphQLQueryBase<TContext> _query;

        public SchemaQueryField(SchemaRootType<TContext> declaringType, GraphQLQueryBase<TContext> query)
        {
            _declaringType = declaringType;
            _query = query;
        }

        public override ISchemaQueryType<Info> DeclaringType => _declaringType;
        public override string FieldName => _query.Name;
        public override ISchemaQueryType<Info> QueryableFieldType => SchemaType.OfType(_query.Type);
        public override Info Info => new Info(_query);
    }
}