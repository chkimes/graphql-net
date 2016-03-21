module public GraphQLParser

open FParsec
open FParsec.CharParsers

type Alias = string option

type Input = Int of int
           | Float of single
           | String of string
           | Boolean of bool
type Argument = string * Input
type Arguments = Argument list

type Directive = string * Arguments option
type Directives = Directive list

type Selection = Field of alias : Alias * name : string * arguments : Arguments * directives : Directives * selectionSet : SelectionSet
and SelectionSet = Selection list

type Query = string * Arguments * SelectionSet

type Definition = QueryOperation of Query
                | MutationOperation
                | Fragment
                | TypeExt
                | TypeDef
                | Enum

type Document = Definition List

let str s = pstring s
let stringLiteral =
    let escape =  anyOf "\"\\/bfnrt"
                  |>> function
                      | 'b' -> "\b"
                      | 'f' -> "\u000C"
                      | 'n' -> "\n"
                      | 'r' -> "\r"
                      | 't' -> "\t"
                      | c   -> string c // every other char is mapped to itself

    let unicodeEscape =
        let hex2int c = (int c &&& 15) + (int c >>> 6)*9

        str "u" >>. pipe4 hex hex hex hex (fun h3 h2 h1 h0 ->
            (hex2int h3)*4096 + (hex2int h2)*256 + (hex2int h1)*16 + hex2int h0
            |> char |> string
        )

    let escapedCharSnippet = str "\\" >>. (escape <|> unicodeEscape)
    let normalCharSnippet  = manySatisfy (fun c -> c <> '"' && c <> '\\')

    between (str "\"") (str "\"")
            (stringsSepBy normalCharSnippet escapedCharSnippet)
let ws = many (skipChar ',' <|> spaces1)

let name =
    let isNameFirstChar c = isLetter c || c = '_'
    let isNameChar c = isLetter c || isDigit c || c = '_'
    many1Satisfy2L isNameFirstChar isNameChar "name" .>> ws

let pnum = numberLiteral (NumberLiteralOptions.AllowExponent ||| NumberLiteralOptions.AllowMinusSign) "number" .>> ws
        |>> fun n ->
            if (n.IsInteger) then Int(int32 n.String)
            else Float(single n.String)
let pbool = str "true" .>> ws |>> (fun a -> Boolean(true)) <|> (str "false" .>> ws |>> (fun a -> Boolean(false)))
let pstr =  stringLiteral .>> ws |>> (fun s -> String(s))
// TODO pguid
let value = pbool <|> pnum <|> pstr

let alias = name .>> str ":" .>> ws
let argument = name .>>. (ws >>. str ":" >>. ws >>. value .>> ws)
let arguments = str "(" >>. ws >>. many argument .>> str ")" .>> ws

let directive = str "@" >>. name .>>. opt (attempt arguments)
let directives = many1 directive

let field, fieldref = createParserForwardedToRef<Selection, unit>()
let selectionset = between (str "{") (str "}") (ws >>. many field) .>> ws

let coalesce optList =
    match optList with
    | Some opts -> opts
    | None -> List.Empty

do fieldref := pipe5
    (opt (attempt alias))
    name
    (opt (attempt arguments))
    (opt (attempt directives))
    (opt (attempt selectionset))
    (fun alias name args dirs set ->
        Field(alias, name, coalesce args, coalesce dirs, coalesce set))
let query =
    pipe3
        (ws >>. str "query" >>. ws >>. name)
        (opt (attempt arguments))
        selectionset
        (fun name args set -> QueryOperation((name, coalesce args, set)))
let parse str = 
        match run query str with
        | Success(result, _, _) -> Some result
        | Failure(errorMsg, _, _) -> None