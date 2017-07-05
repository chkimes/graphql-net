using System;
using GraphQL.Parser;

namespace GraphQL.Net
{
    public interface IGraphQLType
    {
        string Name { get; }
        Type CLRType { get; }
        TypeKind TypeKind { get; }
    }
}