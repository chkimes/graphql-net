using System;
using Microsoft.FSharp.Core;

namespace GraphQL.Net
{
    public static class InteropHelpers
    {
        public static T OrDefault<T>(this FSharpOption<T> option)
            => option == null ? default(T) : option.Value;

        public static FSharpOption<T2> Map<T1, T2>(this FSharpOption<T1> opt, Func<T1, T2> f)
        {
            return OptionModule.Map(
                FSharpFunc<T1, T2>.FromConverter(a => f(a)),
                opt
            );
        }
    }
}
