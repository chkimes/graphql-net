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

namespace GraphQL.Parser
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
    member this.ToObject() =
        match this with
        | IntPrimitive i -> box i
        | FloatPrimitive f -> box f
        | StringPrimitive s -> box s
        | BooleanPrimitive b -> box b
and PrimitiveType =
    | IntType
    | FloatType
    | StringType
    | BooleanType
    member this.AcceptsPrimitive(input : PrimitiveType) =
        this = input
        || this = FloatType && input = IntType

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

/// Represents an estimate of the complexity of a query operation, in
/// arbitrary units of work.
type Complexity =
    | Range of (int64 * int64)
    | Exactly of int64
    member this.Minimum =
        match this with
        | Exactly x -> x
        | Range(min, max) -> min
    member this.Maximum =
        match this with
        | Exactly x -> x
        | Range(_, max) -> max
    static member Zero = Exactly 0L
    static member One = Exactly 1L
    static member Of(x) = Exactly (int64 x)
    static member Of(min, max) = Range(int64 min, int64 max)
    static member private Combine(f, left, right) =
        match left, right with
        | Exactly c1, Exactly c2 -> Exactly (f c1 c2)
        | Range(low1, high1), Exactly c2 ->
            Range(f low1 c2, f high1 c2)
        | Range(low1, high1), Range(low2, high2) ->
            Range(f low1 low2, f high1 high2)
        | Exactly c1, Range(low2, high2) ->
            Range(f c1 low2, f c1 high2)
    static member (*) (left, right) = Complexity.Combine(Checked.(*), left, right)
    static member (+) (left, right) = Complexity.Combine(Checked.(+), left, right)

type ListWithSource<'a> = IReadOnlyList<'a WithSource>

type ISchemaInfo<'s> =
    abstract member Info : 's

/// Represents type information provided by the schema implementation for validation
/// and for inclusion in the validated AST.
type ISchemaQueryType<'s> =
    inherit ISchemaInfo<'s>
    abstract member TypeName : string
    abstract member Description : string option
    /// Get the fields of this type, keyed by name.
    /// May be empty, for example if the type is a primitive.
    abstract member Fields : IReadOnlyDictionary<string, ISchemaField<'s>>
/// Represents a named core type, e.g. a "Time" type represented by an ISO-formatted string.
/// The type may define validation rules that run on values after they have been checked to
/// match the given core type.
and ISchemaVariableType =
    abstract member TypeName : string
    abstract member CoreType : CoreVariableType
    abstract member ValidateValue : Value -> bool
/// Represents the type of a field, which may be either another queryable type, or
/// a non-queryable value.
and SchemaFieldType<'s> =
    | QueryField of ISchemaQueryType<'s>
    | ValueField of VariableType
/// Represents field information provided by the schema implementation for validation
/// and for inclusion in the validated AST.
and ISchemaField<'s> =
    inherit ISchemaInfo<'s>
    abstract member DeclaringType : ISchemaQueryType<'s>
    abstract member FieldType : SchemaFieldType<'s>
    abstract member FieldName : string
    abstract member Description : string option
    /// Get the possible arguments of this field, keyed by name.
    /// May be empty if the field accepts no arguments.
    abstract member Arguments : IReadOnlyDictionary<string, ISchemaArgument<'s>>
    /// Given the arguments (without values) supplied to this field, give a ballpark estimate of
    /// how much work it will take to select.
    abstract member EstimateComplexity : ISchemaArgument<'s> seq -> Complexity
/// Represents argument information provided by the schema implementation for validation
/// and for inclusion in the validated AST.
and ISchemaArgument<'s> =
    inherit ISchemaInfo<'s>
    abstract member ArgumentName : string
    abstract member ArgumentType : VariableType
    abstract member Description : string option
and ISchemaDirective<'s> =
    inherit ISchemaInfo<'s>
    abstract member DirectiveName : string
    abstract member Description : string option
    /// Get the possible arguments of this directive, keyed by name.
    /// May be empty if the directive accepts no arguments.
    abstract member Arguments : IReadOnlyDictionary<string, ISchemaArgument<'s>>
and ISchema<'s> =
    /// Return the core type, if any, with the given name.
    /// A core type is a type whose values can be expressed as a `Value` within
    /// a GraphQL document. This encompasses the values that can be provided as
    /// arguments to a field or directive or declared as variables for an operation.
    abstract member ResolveVariableTypeByName : string -> CoreVariableType option
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
    member this.GetString() =
        match this with
        | NullValue -> null
        | PrimitiveValue (StringPrimitive s) -> s
        | _ -> failwith "Value is not a string"
    member this.GetInteger() =
        match this with
        | PrimitiveValue (IntPrimitive i) -> i
        | _ -> failwith "Value is not an integer"
    member this.GetNumber() =
        match this with
        | PrimitiveValue (FloatPrimitive f) -> f
        | PrimitiveValue (IntPrimitive i) -> double i
        | _ -> failwith "Value is not a number"
    member this.GetBoolean() =
        match this with
        | PrimitiveValue (BooleanPrimitive b) -> b
        | _ -> failwith "Value is not a boolean"
    member this.ToExpression() =
        match this with
        | PrimitiveValue p -> PrimitiveExpression p
        | NullValue -> NullExpression
        | EnumValue e -> EnumExpression e
        | ListValue lst ->
            lst
            |> mapWithSource (fun v -> v.ToExpression())
            |> toReadOnlyList
            |> ListExpression
        | ObjectValue fields ->
            fields
            |> mapDictionaryWithSource (fun v -> v.ToExpression())
            |> ObjectExpression
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
    member this.TryGetValue(resolveVariable) =
        match this with
        | VariableExpression vdef -> resolveVariable vdef.VariableName
        | PrimitiveExpression prim -> Some (PrimitiveValue prim)
        | NullExpression -> Some NullValue
        | EnumExpression en -> Some (EnumValue en)
        | ListExpression lst ->
            let mappedList =
                lst
                |> chooseWithSource (fun v -> v.TryGetValue(resolveVariable))
                |> toReadOnlyList
            if mappedList.Count < lst.Count
            then None
            else Some (ListValue mappedList)
        | ObjectExpression fields ->
            let mappedFields =
                seq {
                    for KeyValue(name, { Source = pos; Value = v }) in fields do
                        match v.TryGetValue(resolveVariable) with
                        | None -> ()
                        | Some res ->
                            yield name, { Source = pos; Value = res }
                } |> dictionary :> IReadOnlyDictionary<_, _>
            if mappedFields.Count < fields.Count
            then None
            else Some (ObjectValue mappedFields)
    member this.ToValue(resolveVariable) : Value =
        match this with
        | VariableExpression vdef ->
            let attempt = resolveVariable vdef.VariableName
            match attempt with
            | Some v ->
                if vdef.VariableType.AcceptsValue(v) then v
                else failwith (sprintf "unacceptable value for variable ``%s''" vdef.VariableName)
            | None ->
                match vdef.DefaultValue with
                | None -> failwith (sprintf "no value provided for variable ``%s''" vdef.VariableName)
                | Some def -> def
        | PrimitiveExpression prim -> PrimitiveValue prim
        | NullExpression -> NullValue
        | EnumExpression en -> EnumValue en
        | ListExpression lst ->
            lst
            |> mapWithSource (fun v -> v.ToValue(resolveVariable))
            |> toReadOnlyList
            |> ListValue
        | ObjectExpression fields ->
            fields
            |> mapDictionaryWithSource (fun v -> v.ToValue(resolveVariable))
            |> ObjectValue 
/// Represents a non-nullable value type.
and CoreVariableType =
    | PrimitiveType of PrimitiveType
    | EnumType of EnumType
    | ListType of VariableType
    /// Not possible to declare this type in a GraphQL document, but it exists nonetheless.
    | ObjectType of IReadOnlyDictionary<string, VariableType>
    | NamedType of ISchemaVariableType
    member internal this.BottomType =
        match this with
        | NamedType n -> n.CoreType
        | _ -> this
    member this.Nullable(yes) = new VariableType(this, yes)
    member this.Nullable() = new VariableType(this, true)
    member this.NotNullable() = new VariableType(this, false)
    member this.TypeName =
        match this with
        | NamedType schemaVariableType -> Some schemaVariableType.TypeName
        | _ -> None
    member this.AcceptsVariableType(vtype : CoreVariableType) =
        this = vtype ||
        match this, vtype with
        | PrimitiveType pTy, PrimitiveType inputTy -> pTy.AcceptsPrimitive(inputTy)
        | NamedType schemaType, vt ->
            schemaType.CoreType.AcceptsVariableType(vt)
        | ListType vt1, ListType vt2 ->
            (vt1.Type : CoreVariableType).AcceptsVariableType(vt2)
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
    member this.AcceptsValue(value : Value) =
        match this, value with
        | NamedType schemaType, value -> schemaType.CoreType.AcceptsValue(value) && schemaType.ValidateValue(value)
        | PrimitiveType pTy, PrimitiveValue pv -> pTy.AcceptsPrimitive(pv.Type)
        | EnumType eType, EnumValue eVal -> eType.EnumName = eVal.Type.EnumName
        | ListType lTy, ListValue vals -> vals |> Seq.forall (fun v -> lTy.AcceptsValue(v.Value))
        | ObjectType oTy, ObjectValue o ->
            seq {
                for KeyValue(name, ty) in oTy do
                    yield
                        match o.TryFind(name) with
                        | None -> false
                        | Some fv -> ty.AcceptsValue(fv.Value)
            } |> Seq.forall id
        | _ -> false
    member this.AcceptsValueExpression(vexpr : ValueExpression) =
        match vexpr.TryGetValue(fun _ -> None) with
        | Some value -> this.AcceptsValue(value)
        | None ->
        match this, vexpr with
        | NamedType schemaType, vexpr -> schemaType.CoreType.AcceptsValueExpression(vexpr)
        | PrimitiveType pTy, PrimitiveExpression pexpr -> pTy.AcceptsPrimitive(pexpr.Type)
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
and VariableType(coreType: CoreVariableType, isNullable : bool) =
    member this.Type = coreType
    member this.Nullable = isNullable
    member this.AcceptsVariableType(vtype : VariableType) =
        this.Type.AcceptsVariableType(vtype)
    member this.AcceptsValueExpression(vexpr : ValueExpression) =
        match vexpr with
        | NullExpression -> this.Nullable
        | notNull -> this.Type.AcceptsValueExpression(notNull)
    member this.AcceptsValue(value : Value) =
        match value with
        | NullValue -> this.Nullable
        | notNull -> this.Type.AcceptsValue(notNull)
and VariableDefinition =
    {
        VariableName : string
        VariableType : VariableType
        DefaultValue : Value option
    }

type ArgumentExpression<'s> =
    {
        Argument : ISchemaArgument<'s>
        Expression : ValueExpression
    }

type Directive<'s> =
    {
        SchemaDirective : ISchemaDirective<'s>
        Arguments : ArgumentExpression<'s> ListWithSource
    }

type Field<'s> =
    {
        SchemaField : ISchemaField<'s>
        Alias : string option
        Arguments : ArgumentExpression<'s> ListWithSource
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
    static member private EstimateComplexity(selections : Selection<'s> ListWithSource) =
        selections
        |> Seq.map (fun s -> s.Value)
        |> Seq.sumBy Operation<'s>.EstimateComplexity 
    static member private EstimateComplexity(selection : Selection<'s>) =
        match selection with
        | FieldSelection field ->
            let fieldComplexity =
                field.SchemaField.EstimateComplexity(field.Arguments |> Seq.map (fun a -> a.Value.Argument))
            let subComplexity =
                Operation<'s>.EstimateComplexity(field.Selections)
            fieldComplexity + fieldComplexity * subComplexity
        | FragmentSpreadSelection spread -> Operation<'s>.EstimateComplexity(spread.Fragment.Selections)
        | InlineFragmentSelection frag -> Operation<'s>.EstimateComplexity(frag.Selections)
    member this.EstimateComplexity() =
        match this with
        | ShorthandOperation sels -> Operation<'s>.EstimateComplexity(sels)
        | LonghandOperation op -> Operation<'s>.EstimateComplexity(op.Selections)

// Note: we don't include fragment definitions in the schema-validated AST.
// This is because the Fragment<'s> type only makes sense where a fragment is
// used via the spread operator in an operation. It's impossible to resolve variable
// types against the schema at the point where a fragment is defined, because they could
// be different depending on where it's used.