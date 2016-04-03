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

namespace GraphQL.Parser.SchemaAST
open GraphQL.Parser
open System
open System.Collections.Generic

type ValidationException(msg : string, pos : SourceInfo) =
    inherit Exception(msg)
    member this.SourceInfo = pos

/// Resolves variables and fragments in the context of a specific operation.
type IOperationContext<'s> =
    abstract member Schema : ISchema<'s>
    abstract member ResolveVariableByName : string -> VariableDefinition<'s> option
    abstract member ResolveFragmentDefinitionByName : string -> ParserAST.Fragment option

module private ResolverUtilities =
    let failAt pos msg =
        new ValidationException(msg, pos) |> raise
    type IOperationContext<'s> with
        member this.ValidateValue(pvalue : ParserAST.Value, pos : SourceInfo) : Value<'s> =
            match pvalue with
            | ParserAST.Variable name ->
                match this.ResolveVariableByName name with
                | Some vdef -> VariableRefValue vdef
                | None -> failAt pos (sprintf "use of undeclared variable ``%s''" name)
            | ParserAST.IntValue i ->
                PrimitiveValue (IntPrimitive i)
            | ParserAST.FloatValue f ->
                PrimitiveValue (FloatPrimitive f)
            | ParserAST.StringValue s ->
                PrimitiveValue (StringPrimitive s)
            | ParserAST.BooleanValue b ->
                PrimitiveValue (BooleanPrimitive b)
            | ParserAST.EnumValue enumName ->
                match this.Schema.ResolveEnumValueByName enumName with
                | None -> failAt pos (sprintf "``%s'' is not a member of any known enum type" enumName)
                | Some enumVal -> EnumValue enumVal
            | ParserAST.ListValue elementsWithSource ->
                [|
                    for element in elementsWithSource do
                        let vvalue = this.ValidateValue(element.Value, element.Source)
                        yield { Value = vvalue; Source = element.Source }
                |] :> IReadOnlyList<_> |> ListValue
            | ParserAST.ObjectValue fieldsWithSource ->
                seq {
                    for KeyValue(fieldName, fieldVal) in fieldsWithSource do
                        let vvalue = this.ValidateValue(fieldVal.Value, fieldVal.Source)
                        yield fieldName, { Value = vvalue; Source = fieldVal.Source }
                } |> dictionary :> IReadOnlyDictionary<_, _> |> ObjectValue
open ResolverUtilities

type Resolver<'s>
    ( schemaType : ISchemaType<'s> // the type being selected from
    , opContext : IOperationContext<'s>
    ) =
    member private this.ResolveArguments
        ( schemaArgs : IReadOnlyDictionary<string, ISchemaArgument<'s>>
        , pargs : ParserAST.Argument WithSource seq
        ) =
        [|
            for { Source = pos; Value = parg } in pargs do
                match schemaArgs.TryFind(parg.ArgumentName) with
                | None -> failAt pos (sprintf "unknown argument ``%s''" parg.ArgumentName)
                | Some arg ->
                    let pargValue = opContext.ValidateValue(parg.ArgumentValue, pos)
                    match arg.ValidateValue(pargValue) with
                    | Invalid reason ->
                        failAt pos (sprintf "invalid argument ``%s'': ``%s''" parg.ArgumentName reason)
                    | Valid argVal ->
                        yield { Value = argVal; Source = pos }
        |] :> IReadOnlyList<_>
    member private this.ResolveDirectives(pdirs : ParserAST.Directive WithSource seq) =
        [|
            for { Source = pos; Value = pdir } in pdirs do
                match opContext.Schema.ResolveDirectiveByName(pdir.DirectiveName) with
                | None -> failAt pos (sprintf "unknown directive ``%s''" pdir.DirectiveName)
                | Some dir ->
                    let args = this.ResolveArguments(dir.Arguments, pdir.Arguments)
                    yield {
                        Value = { SchemaDirective = dir; Arguments = args }
                        Source = pos
                    }
        |] :> IReadOnlyList<_>
    member private this.ResolveFieldSelection(pfield : ParserAST.Field, pos : SourceInfo) =
        match schemaType.Fields.TryFind(pfield.FieldName) with
        | None -> failAt pos (sprintf "``%s'' is not a field of type ``%s''" pfield.FieldName schemaType.TypeName)
        | Some fieldInfo ->
            let directives = this.ResolveDirectives(pfield.Directives)
            let arguments = this.ResolveArguments(fieldInfo.Arguments, pfield.Arguments)
            let child = new Resolver<'s>(fieldInfo.FieldType, opContext)
            {
                SchemaField = fieldInfo
                Alias = pfield.Alias
                Directives = directives
                Arguments = arguments
                Selections = child.ResolveSelections(pfield.Selections)
            }
    member private this.ResolveTypeCondition(typeName : string, pos : SourceInfo) =
        match opContext.Schema.ResolveTypeByName(typeName) with
        | None -> failAt pos (sprintf "unknown type ``%s'' in type condition" typeName)
        | Some ty -> ty
    member private this.ResolveFragment(pfrag : ParserAST.Fragment, pos : SourceInfo) =
        let directives = this.ResolveDirectives(pfrag.Directives)
        let selections = this.ResolveSelections(pfrag.Selections)
        let typeCondition = this.ResolveTypeCondition(pfrag.TypeCondition, pos)
        {
            FragmentName = pfrag.FragmentName
            TypeCondition = typeCondition
            Directives = directives
            Selections = selections
        }
    member private this.ResolveFragmentSpreadSelection
        (pspread : ParserAST.FragmentSpread, pos : SourceInfo) =
        match opContext.ResolveFragmentDefinitionByName(pspread.FragmentName) with
        | None -> failAt pos (sprintf "unknown fragment ``%s''" pspread.FragmentName)
        | Some pfrag ->
            let frag = this.ResolveFragment(pfrag, pos)
            {
                Fragment = frag
                Directives = this.ResolveDirectives(pspread.Directives)
            }
    member private this.ResolveInlineFragment
        (pinline : ParserAST.InlineFragment, pos : SourceInfo) =
        let directives = this.ResolveDirectives(pinline.Directives)
        let selections = this.ResolveSelections(pinline.Selections)
        let typeCondition =
            match pinline.TypeCondition with
            | None -> None
            | Some typeName -> Some <| this.ResolveTypeCondition(typeName, pos)
        {
            TypeCondition = typeCondition
            Directives = directives
            Selections = selections
        }
    member private this.ResolveSelection(pselection : ParserAST.Selection, pos : SourceInfo) =
        match pselection with
        | ParserAST.FieldSelection pfield ->
            this.ResolveFieldSelection(pfield, pos)
            |> FieldSelection
        | ParserAST.FragmentSpreadSelection pfragmentSpread ->
            this.ResolveFragmentSpreadSelection(pfragmentSpread, pos)
            |> FragmentSpreadSelection
        | ParserAST.InlineFragmentSelection pinlineFragment ->
            this.ResolveInlineFragment(pinlineFragment, pos)
            |> InlineFragmentSelection
    member this.ResolveSelections(pselections : ParserAST.Selection WithSource seq) =
        [|
            for { Source = pos; Value = pselection } in pselections do
                yield { Source = pos; Value = this.ResolveSelection(pselection, pos) }
        |] :> IReadOnlyList<_>