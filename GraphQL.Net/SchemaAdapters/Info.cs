namespace GraphQL.Net.SchemaAdapters
{
    class Info
    {
        public Info(GraphQLField field)
        {
            Field = field;
        }

        public Info(GraphQLType type)
        {
            Type = type;
        }

        public GraphQLField Field { get; }
        public GraphQLType Type { get; }
    }
}