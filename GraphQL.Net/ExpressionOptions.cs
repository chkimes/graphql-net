using System;

namespace GraphQL.Net
{
    internal class ExpressionOptions
    {
        public ExpressionOptions(Func<Type, bool> validForQueryType, bool castAssignment = false,
            bool nullCheckLists = false, bool typeCheckInheritance = false, bool useBaseType = false)
        {
            ValidForQueryType = validForQueryType;
            CastAssignment = castAssignment;
            NullCheckLists = nullCheckLists;
            UseBaseType = useBaseType;
            TypeCheckInheritance = typeCheckInheritance;
        }

        public Func<Type, bool> ValidForQueryType { get; }
        public bool CastAssignment { get; }
        public bool NullCheckLists { get; }
        public bool UseBaseType { get; }
        public bool TypeCheckInheritance { get; }
    }
}