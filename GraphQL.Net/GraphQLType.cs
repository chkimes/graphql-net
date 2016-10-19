using System;
using System.Collections.Generic;

namespace GraphQL.Net
{
    internal class GraphQLType
    {
        public GraphQLType(Type type)
        {
            CLRType = type;
            Name = type.Name;
            Fields = new List<GraphQLField>();
            IncludedTypes = new List<GraphQLType>();
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public List<GraphQLField> Fields { get; set; }
        public List<GraphQLType> IncludedTypes { get; set; }
        public Type CLRType { get; set; }
        public Type QueryType { get; set; }
        public bool IsScalar { get; set; } // TODO: TypeKind?
    }
}