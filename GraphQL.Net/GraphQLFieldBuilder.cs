namespace GraphQL.Net
{
    public class GraphQLFieldBuilder
    {
        private readonly GraphQLField _field;

        internal GraphQLFieldBuilder(GraphQLField field)
        {
            _field = field;
        }

        public GraphQLFieldBuilder WithDescription(string description)
        {
            _field.Description = description;
            return this;
        }
    }
}