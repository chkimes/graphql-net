namespace GraphQL.Parser.Execution
open GraphQL.Parser
open System.Runtime.CompilerServices

type ExecArgument<'s> =
    {
        Argument : ISchemaArgument<'s>
        Value : Value
    }

type ExecDirective<'s> =
    {
        SchemaDirective : ISchemaDirective<'s>
        Arguments : ExecArgument<'s> ListWithSource
    }

type ExecSelection<'s> =
    {
        SchemaField : ISchemaField<'s>
        Alias : string option
        TypeCondition : ISchemaQueryType<'s> option
        Arguments : ExecArgument<'s> ListWithSource
        Directives : ExecDirective<'s> ListWithSource
        Selections : ExecSelection<'s> ListWithSource
    }

type IExecContext<'s> =
    abstract member GetVariableValue : string -> Value option

module private Execution =
    let execArgument (context : IExecContext<'s>) ({ Source = pos; Value = { Argument = argument; Expression = expr } }) =
        let getVariable name =
            match context.GetVariableValue(name) with
            | None ->
                failAt pos (sprintf "no value provided for variable ``%s''" name)
            | Some value -> value
        let argValue = expr.ToValue(getVariable)
        if not <| argument.ArgumentType.AcceptsValue(argValue) then
            failAt pos (sprintf "unacceptable value for argument ``%s''" argument.ArgumentName)
        {
            Source = pos
            Value =
            {
                Argument = argument
                Value = argValue
            }
        }
    let execDirective (context : IExecContext<'s>) (directive : Directive<'s>) =
        {
            SchemaDirective = directive.SchemaDirective
            Arguments = directive.Arguments |> Seq.map (execArgument context) |> toReadOnlyList
        }
    let rec execSelections (context : IExecContext<'s>) (selection : Selection<'s> WithSource) =
        match selection.Value with
        | FieldSelection field ->
            {
                SchemaField = field.SchemaField
                Alias = field.Alias
                TypeCondition = None
                Arguments =
                    field.Arguments |> Seq.map (execArgument context) |> toReadOnlyList
                Directives =
                    field.Directives |> mapWithSource (execDirective context) |> toReadOnlyList
                Selections =
                    field.Selections
                    |> Seq.collect (execSelections context)
                    |> toReadOnlyList
            } |> (fun s -> { Source = selection.Source; Value = s }) |> Seq.singleton
        | FragmentSpreadSelection spread ->
            let spreadDirs = spread.Directives |> mapWithSource (execDirective context) |> toReadOnlyList
            let subMap (sel : ExecSelection<'s>) = { sel with Directives = appendReadOnlyList sel.Directives spreadDirs }
            spread.Fragment.Selections
            |> Seq.collect (execSelections context >> mapWithSource subMap)
        | InlineFragmentSelection frag ->
            let fragDirs = frag.Directives |> mapWithSource (execDirective context) |> toReadOnlyList
            let subMap (sel : ExecSelection<'s>) =
                { sel with
                    Directives = appendReadOnlyList sel.Directives fragDirs
                    TypeCondition = match sel.TypeCondition with None -> frag.TypeCondition | Some _ -> sel.TypeCondition
                }
            frag.Selections
            |> Seq.collect (execSelections context >> mapWithSource subMap)