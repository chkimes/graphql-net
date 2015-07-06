module public GraphQLParser

open FParsec
open FParsec.CharParsers

type Alias = string option

type Argument = string * string
type Arguments = Argument list

type Directive = string * string option
type Directives = Directive list

type Selection = Field of alias : Alias * name : string * arguments : Arguments * directives : Directives * selectionSet : SelectionSet
and SelectionSet = Selection list

type Query = string * SelectionSet

type Definition = QueryOperation of Query
                | MutationOperation
                | Fragment
                | TypeExt
                | TypeDef
                | Enum

type Document = Definition List

let str s = pstring s
let ws = many ((str "," >>= (fun a -> preturn ())) <|> spaces1)

let name =
    let isNameFirstChar c = isLetter c || c = '_'
    let isNameChar c = isLetter c || isDigit c || c = '_'
    many1Satisfy2L isNameFirstChar isNameChar "name" .>> ws

let value = name // todo fix

let alias = name .>> str ":" .>> ws
let argument = name .>>. (ws >>. str ":" >>. ws >>. value .>> ws)
let arguments = str "(" >>. ws >>. many argument .>> str ")" .>> ws

let directive = str "@" >>. name .>>. opt (attempt (str ":" >>. ws >>. value))
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
    pipe2
        (ws >>. str "query" >>. ws >>. name)
        selectionset
        (fun name set -> QueryOperation((name, set)))
let parse str = 
        match run query str with
        | Success(result, _, _) -> Some result
        | Failure(errorMsg, _, _) -> None