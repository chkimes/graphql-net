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
open System.Collections.Generic

// This file implements an enhanced form of the AST with information added
// by validating it both for internal consistency and consistency with a schema.
// Generic types in this module with a `'s` generic parameter contain arbitrary information
// supplied by the schema implementation. For example, when the schema implementation is asked
// to resolve a type name, it can provide an `'s` with the resulting type information,
// which will be included in the AST for later use.

/// A primitive value, which needs no extra information.
type Primitive =
    | IntPrimitive of int64
    | FloatPrimitive of double
    | StringPrimitive of string
    | BooleanPrimitive of bool
    member this.Type =
        match this with
        | IntPrimitive _ -> IntType
        | FloatPrimitive _ -> FloatType
        | StringPrimitive _ -> StringType
        | BooleanPrimitive _ -> BooleanType
and PrimitiveType =
    | IntType
    | FloatType
    | StringType
    | BooleanType

type EnumTypeValue =
    {
        ValueName : string
        Description : string option
    }
and EnumType =
    {
        EnumName : string
        Description : string option
        Values : IReadOnlyDictionary<string, EnumTypeValue>
    }

type EnumValue =
    {
        Type : EnumType
        Value : EnumTypeValue
    }

type ListWithSource<'a> = IReadOnlyList<'a WithSource>

/// Represents type information provided by the schema implementation for validation
/// and for inclusion in the validated AST.
type ISchemaQueryType<'s> =
    abstract member TypeName : string
    abstract member Description : string option
    abstract member Info : 's
    /// Get the fields of this type, keyed by name.
    /// May be empty, for example if the type is a primitive.
    abstract member Fields : IReadOnlyDictionary<string, ISchemaField<'s>>
/// Represents a named core type, e.g. a "Time" type represented by an ISO-formatted string.
/// The type may define validation rules that run on values after they have been checked to
/// match the given core type.
and ISchemaVariableType =
    abstract member TypeName : string
    abstract member CoreType : CoreVariableType
    /// Produce an error message if the value is not valid for this type.
    abstract member ValidateValue : Value -> string option
/// Represents the type of a field, which may be either another queryable type, or
/// a non-queryable value.
and SchemaFieldType<'s> =
    | QueryField of ISchemaQueryType<'s>
    | ValueField of VariableType
/// Represents field information provided by the schema implementation for validation
/// and for inclusion in the validated AST.
and ISchemaField<'s> =
    abstract member DeclaringType : ISchemaQueryType<'s>
    abstract member FieldType : SchemaFieldType<'s>
    abstract member FieldName : string
    abstract member Description : string option
    abstract member Info : 's
    /// Get the possible arguments of this field, keyed by name.
    /// May be empty if the field accepts no arguments.
    abstract member Arguments : IReadOnlyDictionary<string, ISchemaArgument<'s>>
/// Represents argument information provided by the schema implementation for validation
/// and for inclusion in the validated AST.
and ISchemaArgument<'s> =
    abstract member ArgumentName : string
    abstract member ArgumentType : CoreVariableType
    abstract member Description : string option
    abstract member Info : 's
and ISchemaDirective<'s> =
    abstract member DirectiveName : string
    abstract member Description : string option
    abstract member Info : 's
    /// Get the possible arguments of this directive, keyed by name.
    /// May be empty if the directive accepts no arguments.
    abstract member Arguments : IReadOnlyDictionary<string, ISchemaArgument<'s>>
and ISchema<'s> =
    /// Return the core type, if any, with the given name.
    /// A core type is a type whose values can be expressed as a `Value` within
    /// a GraphQL document. This encompasses the values that can be provided as
    /// arguments to a field or directive or declared as variables for an operation.
    abstract member ResolveVariableTypeByName : string -> ISchemaVariableType option
    /// Return the type, if any, with the given name. These are types that
    /// may appear in a query and 
    abstract member ResolveQueryTypeByName : string -> ISchemaQueryType<'s> option
    /// Return all types that contain the given enum value name.
    abstract member ResolveEnumValueByName : string -> EnumValue option
    /// Return the directive, if any, with the given name.
    abstract member ResolveDirectiveByName : string -> ISchemaDirective<'s> option
    /// The top-level type that queries select from.
    /// Most likely this will correspond to your DB context type.
    abstract member RootType : ISchemaQueryType<'s>
/// A value within the GraphQL document. This is fully resolved, not a variable reference.
and Value =
    | PrimitiveValue of Primitive
    | NullValue
    | EnumValue of EnumValue
    | ListValue of Value ListWithSource
    | ObjectValue of IReadOnlyDictionary<string, Value WithSource>
    member this.ToExpression() =
        match this with
        | PrimitiveValue p -> PrimitiveExpression p
        | NullValue -> NullExpression
        | EnumValue e -> EnumExpression e
        | ListValue lst ->
            [| 
                for { Source = pos; Value = v } in lst do
                    yield { Source = pos; Value = v.ToExpression() }
            |] :> IReadOnlyList<_> |> ListExpression
        | ObjectValue o ->
            [|
                for KeyValue(name, { Source = pos; Value = v }) in o do
                    yield name, { Source = pos; Value = v.ToExpression() }
            |] |> dictionary :> IReadOnlyDictionary<_, _> |> ObjectExpression
/// A value expression within the GraphQL document.
/// This may contain references to variables, whose values are not yet
/// supplied.
and ValueExpression =
    | VariableExpression of VariableDefinition
    | PrimitiveExpression of Primitive
    | NullExpression
    | EnumExpression of EnumValue
    | ListExpression of ValueExpression ListWithSource
    | ObjectExpression of IReadOnlyDictionary<string, ValueExpression WithSource>
/// Represents a non-nullable value type.
and CoreVariableType =
    | PrimitiveType of PrimitiveType
    | EnumType of EnumType
    | ListType of VariableType
    /// Not possible to declare this type in a GraphQL document, but it exists nonetheless.
    | ObjectType of IReadOnlyDictionary<string, VariableType>
    | NamedType of ISchemaVariableType
    member this.AcceptsVariableType(vtype : CoreVariableType) =
        this = vtype ||
        match this, vtype with
        | NamedType schemaType, vt ->
            schemaType.CoreType.AcceptsVariableType(vt)
        | ListType vt1, ListType vt2 ->
            vt1.Type.AcceptsVariableType(vt2)
        | ObjectType o1, ObjectType o2 ->
            seq {
                for KeyValue(name, vt1) in o1 do
                    yield
                        match o2.TryFind(name) with
                        | None -> false
                        | Some vt2 -> vt1.AcceptsVariableType(vt2)
            } |> Seq.forall id
        | _ -> false
    member this.AcceptsVariableType(vtype : VariableType) =
        this.AcceptsVariableType(vtype.Type)
    member this.AcceptsValueExpression(vexpr : ValueExpression) =
        match this, vexpr with
        | NamedType schemaType, vexpr -> schemaType.CoreType.AcceptsValueExpression(vexpr)
        | PrimitiveType pTy, PrimitiveExpression pexpr -> pTy = pexpr.Type
        | EnumType eType, EnumExpression eVal -> eType.EnumName = eVal.Type.EnumName
        | ListType lTy, ListExpression vals -> vals |> Seq.forall (fun v -> lTy.AcceptsValueExpression(v.Value))
        | ObjectType oTy, ObjectExpression o ->
            seq {
                    for KeyValue(name, ty) in oTy do
                        yield
                            match o.TryFind(name) with
                            | None -> false
                            | Some fv -> ty.AcceptsValueExpression(fv.Value)
            } |> Seq.forall id
        | _ -> false
and VariableType =
    {
        Type : CoreVariableType
        Nullable : bool
    }
    member this.AcceptsVariableType(vtype : VariableType) =
        this.Type.AcceptsVariableType(vtype)
    member this.AcceptsValueExpression(vexpr : ValueExpression) =
        match vexpr with
        | NullExpression -> this.Nullable
        | notNull -> this.Type.AcceptsValueExpression(notNull)
and VariableDefinition =
    {
        VariableName : string
        VariableType : VariableType
        DefaultValue : Value option
    }

type ArgumentValue<'s> =
    {
        Argument : ISchemaArgument<'s>
        Value : ValueExpression
    }

type Directive<'s> =
    {
        SchemaDirective : ISchemaDirective<'s>
        Arguments : ArgumentValue<'s> ListWithSource
    }

type Field<'s> =
    {
        SchemaField : ISchemaField<'s>
        Alias : string option
        Arguments : ArgumentValue<'s> ListWithSource
        Directives : Directive<'s> ListWithSource
        Selections : Selection<'s> ListWithSource
    }
and Selection<'s> =
    | FieldSelection of Field<'s>
    | FragmentSpreadSelection of FragmentSpread<'s>
    | InlineFragmentSelection of InlineFragment<'s>
and FragmentSpread<'s> =
    {
        Fragment : Fragment<'s>
        Directives : Directive<'s> ListWithSource
    }
and Fragment<'s> =
    {
        FragmentName : string
        TypeCondition : ISchemaQueryType<'s>
        Directives : Directive<'s> ListWithSource
        Selections : Selection<'s> ListWithSource
    }
and InlineFragment<'s> =
    {
        TypeCondition : ISchemaQueryType<'s> option
        Directives : Directive<'s> ListWithSource
        Selections : Selection<'s> ListWithSource
    }

type OperationType =
    | Query
    | Mutation

type LonghandOperation<'s> =
    {
        OperationType : OperationType
        OperationName : string option
        VariableDefinitions : VariableDefinition ListWithSource
        Directives : Directive<'s> ListWithSource
        Selections : Selection<'s> ListWithSource
    }

type Operation<'s> =
    | ShorthandOperation of Selection<'s> ListWithSource
    | LonghandOperation of LonghandOperation<'s>

// Note: we don't include fragment definitions in the schema-validated AST.
// This is because the Fragment<'s> type only makes sense where a fragment is
// used via the spread operator in an operation. It's impossible to resolve variable
// types against the schema at the point where a fragment is defined, because they could
// be different depending on where it's used.