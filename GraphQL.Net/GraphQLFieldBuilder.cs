using System;
using GraphQL.Parser;

namespace GraphQL.Net
{
    public class GraphQLFieldBuilder<TContext, TField>
    {
        private readonly GraphQLField _field;

        internal GraphQLFieldBuilder(GraphQLField field)
        {
            _field = field;
        }

        public GraphQLFieldBuilder<TContext, TField> WithDescription(string description)
        {
            _field.Description = description;
            return this;
        }

        public GraphQLFieldBuilder<TContext, TField> WithComplexity(long min, long max)
        {
            _field.Complexity = Complexity.NewRange(Tuple.Create(min, max));
            return this;
        }

        public GraphQLFieldBuilder<TContext, TField> WithReturnType(string graphQlTypeName)
        {
            _field.SetReturnType(_field.Schema.GetGQLTypeByName(graphQlTypeName));
            return this;
        }
        
        internal GraphQLFieldBuilder<TContext, TField> WithResolutionType(ResolutionType type)
        {
            _field.ResolutionType = type;
            
            if (_field.IsList && (type == ResolutionType.First || type == ResolutionType.FirstOrDefault))
            {
                _field.IsList = false;
            }
            return this;
        }
    }
}