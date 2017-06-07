using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Parser;

namespace GraphQL.Net
{
    internal class GraphQLType
    {
        public GraphQLType(Type type)
        {
            CLRType = type;
            Name = type.Name;
            OwnFields = new List<GraphQLField>();
            IncludedTypes = new List<GraphQLType>();
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public List<GraphQLField> OwnFields { get; set; }
        public GraphQLType BaseType { get; set; }
        public List<GraphQLType> IncludedTypes { get; set; }
        public Type CLRType { get; set; }
        public Type QueryType { get; set; }
        public TypeKind TypeKind { get; set; }

        // Returns own fields and the fields of all included types.
        public IEnumerable<GraphQLField> GetQueryFields()
        {
            return OwnFields.Concat(IncludedTypes.SelectMany(t => t.GetQueryFields()).Where(f => f.Name != "__typename"));
        }

        // Returns own fields and the fields of all included types.
        public IEnumerable<GraphQLField> GetAllFieldIncludeBaseType()
        {
            return BaseType != null ? OwnFields.Concat(BaseType.GetAllFieldIncludeBaseType()) : OwnFields;
        }
    }
}