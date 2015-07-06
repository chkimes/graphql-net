open FParsec
open FParsec.CharParsers
open GraphQLParser

[<EntryPoint>]
let main argv = 
    let test p str =
        match run p str with
        | Success(result, _, _) -> printfn "Success: %A" result
        | Failure(errorMsg, _, _) -> printfn "Failure: %s" errorMsg

    test query "
    query test {
        field1 : aliased (id: x) @directive @directive:value {
            nestedField
            anotherNested
        }
        field2
        field3 {
            moreNests
        }
    }"
    System.Console.ReadLine() |> ignore
    0 // return an integer exit code
