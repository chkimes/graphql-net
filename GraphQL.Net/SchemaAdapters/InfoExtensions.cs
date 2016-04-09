using GraphQL.Parser;

namespace GraphQL.Net.SchemaAdapters
{
    static class InfoExtensions
    {
        public static GraphQLField Field(this ISchemaInfo<Info> schemaInfo) => schemaInfo.Info?.Field;
    }
}