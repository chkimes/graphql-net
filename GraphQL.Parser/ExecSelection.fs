//MIT License
//
//Copyright (c) 2016 Robert Peele
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

namespace GraphQL.Parser.Execution
open GraphQL.Parser
open System.Runtime.CompilerServices

// This modules defines types that represent a selection in a way easier for code executing a query to deal with.

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

type IExecContext =
    abstract member GetVariableValue : string -> Value option

type DefaultExecContext() =
    interface IExecContext with
        member this.GetVariableValue(_) = None
    static member Instance = new DefaultExecContext() :> IExecContext
    

module private Execution =
    let execArgument (context : IExecContext) ({ Source = pos; Value = { Argument = argument; Expression = expr } }) =
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
    let execDirective (context : IExecContext) (directive : Directive<'s>) =
        {
            SchemaDirective = directive.SchemaDirective
            Arguments = directive.Arguments |> Seq.map (execArgument context) |> toReadOnlyList
        }
    let rec execSelections (context : IExecContext) (selection : Selection<'s> WithSource) =
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

[<Extension>]
type ExecContextExtensions =
    [<Extension>]
    static member ToExecSelections(context : IExecContext, selection : Selection<'s> WithSource) =
        Execution.execSelections context selection
    [<Extension>]
    static member ToExecSelections(context : IExecContext, selections : Selection<'s> ListWithSource) =
        selections |> Seq.collect (Execution.execSelections context)
    [<Extension>]
    static member ToExecSelections(context : IExecContext, operation : Operation<'s>) =
        match operation with
        | ShorthandOperation sels -> ExecContextExtensions.ToExecSelections(context, sels)
        // TODO propagate directives from top level
        | LonghandOperation op -> ExecContextExtensions.ToExecSelections(context, op.Selections)