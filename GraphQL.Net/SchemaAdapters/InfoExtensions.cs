using GraphQL.Parser;

namespace GraphQL.Net.SchemaAdapters
{
    static class InfoExtensions
    {
        public static GraphQLField Field(this ISchemaInfo<Info> schemaInfo) => schemaInfo.Info?.Field;
        public static GraphQLType Type(this ISchemaInfo<Info> schemaInfo) => schemaInfo.Info?.Type;
        public static GraphQLQueryBase Query(this ISchemaInfo<Info> schemaInfo) => schemaInfo.Info?.Query;
    }
}