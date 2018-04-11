using System;
using System.Collections.Generic;
using GraphQL.Parser;

namespace GraphQL.Net
{
    internal class GraphQLType
    {
        public GraphQLType(Type type)
        {
            CLRType = type;
            Name = type.Name;
            Fields = new List<GraphQLField>();
            PossibleCLRTypes = new List<Type>();
            PossibleTypes = new List<GraphQLType>();
            Interfaces = new List<GraphQLType>();
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public List<GraphQLField> Fields { get; set; }
        public List<Type> PossibleCLRTypes { get; set; }
        public List<GraphQLType> PossibleTypes { get; set; }
        public List<GraphQLType> Interfaces { get; set; }
        public Type CLRType { get; set; }
        public Type QueryType { get; set; }
        public TypeKind TypeKind { get; set; }

        public IEnumerable<GraphQLField> GetQueryFields()
        {
            return Fields;
        }
    }
}