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
open System
open System.Reflection
open System.Collections.Generic

type TypeMapping(clrType : Type, varType : VariableType, translate : Value -> obj) =
    member this.CLRType = clrType
    member this.VariableType = varType
    member this.Translate = translate
    static member Integer<'a>(translate : int64 -> 'a) =
        new TypeMapping(typeof<'a>, (PrimitiveType IntType).NotNullable()
            , fun (v : Value) -> v.GetInteger() |> translate |> box)
    static member Float<'a>(translate : double -> 'a) =
        new TypeMapping(typeof<'a>, (PrimitiveType FloatType).NotNullable()
            , fun (v : Value) -> v.GetFloat() |> translate |> box)
    static member String<'a>(translate : string -> 'a) =
        new TypeMapping(typeof<'a>, (PrimitiveType StringType).NotNullable()
            , fun (v : Value) -> v.GetString() |> translate |> box)
    static member Boolean<'a>(translate : bool -> 'a) =
        new TypeMapping(typeof<'a>, (PrimitiveType BooleanType).NotNullable()
            , fun (v : Value) -> v.GetBoolean() |> translate |> box)

type ITypeHandler =
    /// Get the types explicitly defined by this handler.
    abstract member DefinedTypes : CoreVariableType seq
    /// Get a mapping for `targetType`, if this handler supports it.
    abstract member GetMapping : targetType : Type -> TypeMapping option

type ZeroTypeHandler() =
    interface ITypeHandler with
        member this.DefinedTypes = upcast []
        member this.GetMapping _ = None

type ChainTypeHandler(primary : ITypeHandler, fallback : ITypeHandler) =
    interface ITypeHandler with
        member this.DefinedTypes = Seq.append primary.DefinedTypes fallback.DefinedTypes
        member this.GetMapping(targetType) =
            match primary.GetMapping(targetType) with
            | Some r -> Some r
            | None -> fallback.GetMapping(targetType)
[<AutoOpen>]
module TypeHandlerExtensions =
    type ITypeHandler with
        member this.VariableTypeOf(targetType) =
            match this.GetMapping(targetType) with
            | Some translator -> translator.VariableType
            | None -> failwith (sprintf "Unsupported CLR type ``%s''" targetType.Name)
        member this.TranslateValueTo(targetType, value) =
            match this.GetMapping(targetType) with
            | Some translator -> translator.Translate(value)
            | None -> failwith (sprintf "Unsupported CLR type ``%s''" targetType.Name)

    let (??>) (primary : #ITypeHandler) (secondary : #ITypeHandler) =
        new ChainTypeHandler(primary, secondary) :> ITypeHandler

    let (??<) (secondary : #ITypeHandler) (primary : #ITypeHandler) =
        new ChainTypeHandler(primary, secondary) :> ITypeHandler

type BuiltinTypeHandler() =
    static let integer (convert : int64 -> 'a) name (min, max) =
        new TypeMapping
            ( typeof<'a>
            ,
                ({ new ISchemaVariableType with
                    member this.TypeName = name
                    member this.CoreType = PrimitiveType IntType
                    member this.ValidateValue(value) =
                        match value with
                        | PrimitiveValue (IntPrimitive i) ->
                            i >= min && i <= max
                        | _ -> false
                } |> NamedType).NotNullable()
            , fun v -> v.GetInteger() |> convert |> box
            )
    static let floating (convert : double -> 'a) name =
        new TypeMapping
            ( typeof<'a>
            ,
                ({ new ISchemaVariableType with
                    member this.TypeName = name
                    member this.CoreType = PrimitiveType FloatType
                    member this.ValidateValue(value) = true
                } |> NamedType).NotNullable()
            , fun v -> v.GetFloat() |> convert |> box
            )
    static let builtinTypes =
        [
            integer int8 "Int8" (int64 SByte.MinValue, int64 SByte.MaxValue)
            integer int16 "Int16" (int64 Int16.MinValue, int64 Int16.MaxValue)
            integer int32 "Int" (int64 Int32.MinValue, int64 Int32.MaxValue)
            integer int64 "Int64" (Int64.MinValue, Int64.MaxValue)
            integer uint8 "UInt8" (int64 Byte.MinValue, int64 Byte.MaxValue)
            integer uint16 "UInt16" (int64 UInt16.MinValue, int64 UInt16.MaxValue)
            integer uint32 "UInt32" (int64 UInt32.MinValue, int64 UInt32.MaxValue)
            integer uint64 "UInt64" (Int64.MinValue, Int64.MaxValue) // maybe we shouldn't support this - representing ulongs with negative #s

            floating double "Float"
            floating single "Float32"
            floating decimal "Decimal"

            TypeMapping.String(id)

            TypeMapping.Boolean(id)
        ] |> Seq.map (fun t -> t.CLRType, t) |> dictionary
    interface ITypeHandler with
        member this.DefinedTypes =
            builtinTypes.Values
            |> Seq.map (fun t -> t.VariableType.Type)
        member this.GetMapping(targetType) = builtinTypes.TryFind(targetType)

/// Handles System.Nullable<T>, where T is supported by `rootHandler`.
type NullableTypeHandler(rootHandler : ITypeHandler) =
    interface ITypeHandler with
        member this.DefinedTypes = upcast []
        member this.GetMapping(ty) =
            if ty.IsValueType
                && ty.IsGenericType
                && ty.GetGenericTypeDefinition() = typedefof<System.Nullable<_>>
            then
                let clrElementType = ty.GetGenericArguments().[0]
                let elementType = rootHandler.VariableTypeOf(clrElementType).Type
                new TypeMapping
                    ( ty
                    , new VariableType(elementType, isNullable = true)
                    , fun value ->
                        if value = NullValue then null
                        else
                            let wrappedValue = rootHandler.TranslateValueTo(clrElementType, value)
                            ty.GetConstructor([|clrElementType|]).Invoke([|wrappedValue|])
                    ) |> Some
            else None

/// Handles T[] arrays, where T is supported by `rootHandler`.
type ArrayTypeHandler(rootHandler : ITypeHandler) =
    let translateArray clrElementType value =
        match value with
        | NullValue -> null
        | ListValue vs ->
            let arr = System.Array.CreateInstance(clrElementType, [|vs.Count|])
            let mutable i = 0
            for { Value = v } in vs do
                arr.SetValue(rootHandler.TranslateValueTo(clrElementType, v), i)
                i <- i + 1
            box arr
        | _ -> failwith "Value not suitable for array initialization"
    interface ITypeHandler with
        member this.DefinedTypes = upcast []
        member this.GetMapping(targetType) =
            if targetType.IsArray then
                let clrElementType = targetType.GetElementType()
                let elementType = rootHandler.VariableTypeOf(clrElementType)
                new TypeMapping
                    ( targetType
                    , (ListType elementType).Nullable()
                    , translateArray clrElementType
                    ) |> Some
            else None

/// Handles types implementing ICollection<T> with public parameterless constructors,
/// where T is supported by `rootHandler`.
type CollectionTypeHandler(rootHandler : ITypeHandler) =
    let translateCollection
        (emptyCons : ConstructorInfo)
        (icollection : Type)
        (clrElementType : Type)
        value =
        let add = icollection.GetMethod("Add", [|clrElementType|])
        match value with
        | NullValue -> null
        | ListValue vs ->
            let collection = emptyCons.Invoke([||])
            for { Value = v } in vs do
                ignore <| add.Invoke(collection, [|rootHandler.TranslateValueTo(clrElementType, v)|])
            collection
        | _ -> failwith "Value not suitable for collection initialization"
    interface ITypeHandler with
        member this.DefinedTypes = upcast []
        member this.GetMapping(targetType) =
            let emptyCons = targetType.GetConstructor([||])
            if isNull emptyCons then None else
            let interfaces = targetType.GetInterfaces()
            let icollection =
                interfaces
                |> Array.tryFind(fun i ->
                    i.IsGenericType && i.GetGenericTypeDefinition() = typedefof<ICollection<_>>)
            match icollection with
            | None -> None
            | Some icollection ->
                let clrElementType = icollection.GetGenericArguments().[0]
                let elementType = rootHandler.VariableTypeOf(clrElementType)
                new TypeMapping
                    ( targetType
                    , (ListType elementType).Nullable(not targetType.IsValueType)
                    , translateCollection emptyCons icollection clrElementType
                    ) |> Some

/// Handles types implementing IEnumerable<T> that have a constructor taking an IEnumerable<T>,
/// where T is supported by `rootHandler`.
type EnumerableTypeHandler(rootHandler : ITypeHandler) =
    let translateEnumerable
        (cons : ConstructorInfo) // constructor taking IEnumerable<clrElementType>
        (clrElementType : Type)
        value =
        let collectionType = typedefof<List<_>>.MakeGenericType([|clrElementType|])
        let collectionAdd = collectionType.GetMethod("Add", [|clrElementType|])
        match value with
        | NullValue -> null
        | ListValue vs ->
            let collection = System.Activator.CreateInstance(collectionType)
            for { Value = v } in vs do
                ignore <| collectionAdd.Invoke(collection, [|rootHandler.TranslateValueTo(clrElementType, v)|])
            cons.Invoke([|collection|])
        | _ -> failwith "Value not suitable for IEnumerable initialization"
    interface ITypeHandler with
        member this.DefinedTypes = upcast []
        member this.GetMapping(targetType) =
            let interfaces = targetType.GetInterfaces()
            let ienumerable =
                interfaces
                |> Array.tryFind(fun i ->
                    i.IsGenericType && i.GetGenericTypeDefinition() = typedefof<IEnumerable<_>>)
            match ienumerable with
            | None -> None
            | Some ienumerable ->
                let clrElementType = ienumerable.GetGenericArguments().[0]
                let elementType = rootHandler.VariableTypeOf(clrElementType)
                let cons = targetType.GetConstructor([|ienumerable|]) // can we be constructed from another IEnumerable<T>?
                if isNull cons then None else
                new TypeMapping
                    ( targetType
                    , (ListType elementType).Nullable(not targetType.IsValueType)
                    , translateEnumerable cons clrElementType
                    ) |> Some

/// Handles CLR types with a single constructor taking 1 or more parameter types,
/// where all parameter types are supported by `rootHandler`.
type SingleConstructorTypeHandler(rootHandler : ITypeHandler) =
    interface ITypeHandler with
        member this.DefinedTypes = upcast []
        member this.GetMapping(targetType) =
            let constructors = targetType.GetConstructors()
            if constructors.Length <> 1 then None else
            let cons = constructors.[0]
            let parameters = cons.GetParameters()
            if parameters.Length < 1 then None else
            let fields =
                [
                    for parameter in parameters do
                        let varTy = rootHandler.VariableTypeOf(parameter.ParameterType)
                        yield parameter.Name, varTy
                ] |> dictionary
            new TypeMapping
                ( targetType
                , (ObjectType fields).Nullable(not targetType.IsValueType)
                ,
                    function
                    | NullValue -> null
                    | ObjectValue fields ->
                        let arguments = Array.create<obj> parameters.Length null
                        let mutable i = 0
                        for parameter in parameters do
                            let value = fields.[parameter.Name].Value
                            let clrValue = rootHandler.TranslateValueTo(parameter.ParameterType, value)
                            arguments.[i] <- clrValue
                        cons.Invoke(arguments)
                    | _ -> failwith "Invalid object fields for constructor"
                ) |> Some

/// Handles `output' types by representing them with `repr' types, where `repr' is supported by `rootHandler'.
type TranslationTypeHandler<'repr, 'output>
    ( rootHandler : ITypeHandler
    , name : string
    , validate : 'repr -> bool
    , translate : 'repr -> 'output
    ) =
    let mapping =
        lazy(
            match rootHandler.GetMapping(typeof<'repr>) with
            | None -> failwith <| sprintf "Unsupported represention type ``%s''" typeof<'repr>.Name
            | Some reprMapping ->
                let getReprValue value : 'repr =
                    reprMapping.Translate(value) |> Unchecked.unbox
                new TypeMapping
                    ( typeof<'output>
                    ,
                        ({ new ISchemaVariableType with
                            member this.TypeName = name
                            member this.CoreType = reprMapping.VariableType.Type
                            member this.ValidateValue(value) = getReprValue value |> validate
                        } |> NamedType).Nullable(reprMapping.VariableType.Nullable)
                    , getReprValue >> translate >> box
                    )
        )
    interface ITypeHandler with
        member this.DefinedTypes = upcast [ mapping.Value.VariableType.Type ]
        member this.GetMapping(targetType) =
            if targetType = typeof<'output> then
                Some mapping.Value
            else None

type IMetaTypeHandler =
    /// Given a reference to the root type handler that will ultimately
    /// handle all supported types, produce a sequence of additional type handlers,
    /// in order of priority ascending (the last handler will be tried first).
    abstract member Handlers : rootHandler : ITypeHandler -> ITypeHandler seq

type RootTypeHandler(metaHandler : IMetaTypeHandler) as this =
    let mappingCache = new Dictionary<Type, TypeMapping option>()
    let context =
        lazy(
            let root = this :> ITypeHandler
            let zero = new ZeroTypeHandler() :> ITypeHandler
            let customHandler =
                Seq.fold (??<) zero (metaHandler.Handlers(root))
            customHandler
            ??> new BuiltinTypeHandler()
            ??> new NullableTypeHandler(root)
            ??> new ArrayTypeHandler(root)
            ??> new CollectionTypeHandler(root)
            ??> new EnumerableTypeHandler(root)
            ??> new SingleConstructorTypeHandler(root)
        )
    static let defaultInstance =
        new RootTypeHandler
            ({ new IMetaTypeHandler with
                member __.Handlers(_) = upcast []
            })
    static member Default = defaultInstance :> ITypeHandler
    interface ITypeHandler with
        member this.DefinedTypes = context.Value.DefinedTypes
        member this.GetMapping(targetType) =
            let mutable cached = None
            if (not <| mappingCache.TryGetValue(targetType, &cached)) then
                cached <- context.Value.GetMapping(targetType)
                mappingCache.[targetType] <- cached
            cached