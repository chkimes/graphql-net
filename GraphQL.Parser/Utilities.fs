[<AutoOpen>]
module GraphQL.Parser.Utilities
open System.Collections.Generic

/// Make a dictionary from a list of key/value pair tuples.
/// This is like the built-in F# function dict, except it returns a System.Collections.Dictionary
/// instead of an IDictionary, and it does *not* allow duplicate keys to implicitly overwrite each
/// other.
let dictionary (pairs : ('k * 'v) seq) =
    let d = new Dictionary<_, _>()
    for key, value in pairs do
        d.Add(key, value)
    d

type IReadOnlyDictionary<'k, 'v> with
    member this.TryFind(key : 'k) =
        let mutable output = Unchecked.defaultof<'v>
        if this.TryGetValue(key, &output) then Some output
        else None