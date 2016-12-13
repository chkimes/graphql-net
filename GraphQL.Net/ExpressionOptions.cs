using System;

namespace GraphQL.Net
{
    internal class ExpressionOptions
    {
        public ExpressionOptions(Func<Type, bool> validForQueryType, bool castAssignment = false, bool nullCheckLists = false, bool typeCheckInheritance = false)
        {
            ValidForQueryType = validForQueryType;
            CastAssignment = castAssignment;
            NullCheckLists = nullCheckLists;
            TypeCheckInheritance = typeCheckInheritance;
        }

        public Func<Type, bool> ValidForQueryType { get; }
        public bool CastAssignment { get; }
        public bool NullCheckLists { get; }
        public bool TypeCheckInheritance { get; }
    }
}