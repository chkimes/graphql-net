namespace GraphQL.Net
{
    public class GraphQLFieldBuilder<TArgs>
    {
        private readonly GraphQLField _field;

        internal GraphQLFieldBuilder(GraphQLField field)
        {
            _field = field;
        }

        public GraphQLFieldBuilder<TArgs> WithDescription(string description)
        {
            _field.Description = description;
            return this;
        }
    }
}