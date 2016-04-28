using Microsoft.FSharp.Core;

namespace GraphQL.Net
{
    public static class InteropHelpers
    {
        public static T OrDefault<T>(this FSharpOption<T> option)
            => option == null ? default(T) : option.Value;
    }
}
