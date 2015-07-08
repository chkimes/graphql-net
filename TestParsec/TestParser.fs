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
    query test (id: 4.5) {
        field1 : aliased (id: 1) @directive @directive(test: 3) {
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
