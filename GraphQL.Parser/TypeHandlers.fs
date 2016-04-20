namespace GraphQL.Parser
open System
open System.Reflection
open System.Collections.Generic

type TypeMapping(clrType : System.Type, varType : VariableType, translate : Value -> obj) =
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
    abstract member GetKnownTypes : CoreVariableType seq
    abstract member GetMapping : targetType : System.Type -> TypeMapping option

type ZeroTypeHandler() =
    interface ITypeHandler with
        member this.GetKnownTypes = upcast []
        member this.GetMapping _ = None

type ChainTypeContext(primary : ITypeHandler, fallback : ITypeHandler) =
    interface ITypeHandler with
        member this.GetKnownTypes = Seq.append primary.GetKnownTypes fallback.GetKnownTypes
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

    let (<??>) (context1 : #ITypeHandler) (context2 : #ITypeHandler) =
        new ChainTypeContext(context1, context2) :> ITypeHandler

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
        member this.GetKnownTypes =
            builtinTypes.Values
            |> Seq.map (fun t -> t.VariableType.Type)
        member this.GetMapping(targetType) = builtinTypes.TryFind(targetType)

type NullableTypeContext(rootHandler : ITypeHandler) =
    interface ITypeHandler with
        member this.GetKnownTypes = upcast []
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

type ArrayTypeContext(rootHandler : ITypeHandler) =
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
        member this.GetKnownTypes = upcast []
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

type CollectionTypeContext(rootHandler : ITypeHandler) =
    let translateCollection
        (emptyCons : ConstructorInfo)
        (icollection : System.Type)
        (clrElementType : System.Type)
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
        member this.GetKnownTypes = upcast []
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

type EnumerableTypeContext(rootHandler : ITypeHandler) =
    let translateEnumerable
        (cons : ConstructorInfo) // constructor taking IEnumerable<clrElementType>
        (clrElementType : System.Type)
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
        member this.GetKnownTypes = upcast []
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

type SingleConstructorTypeContext(rootHandler : ITypeHandler) =
    interface ITypeHandler with
        member this.GetKnownTypes = upcast []
        member this.GetMapping(targetType) =
            let constructors = targetType.GetConstructors()
            if constructors.Length <> 1 then None else
            let cons = constructors.[0]
            let parameters = cons.GetParameters()
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

type RootTypeHandler(customContext : ITypeHandler -> ITypeHandler) as this =
    let mutable context : ITypeHandler = Unchecked.defaultof<ITypeHandler>
    do
        context <-
            customContext(this :> ITypeHandler) // custom types get first precedence
            <??> new BuiltinTypeHandler()
            <??> new NullableTypeContext(this :> ITypeHandler)
            <??> new ArrayTypeContext(this :> ITypeHandler)
            <??> new CollectionTypeContext(this :> ITypeHandler)
            <??> new EnumerableTypeContext(this :> ITypeHandler)
            <??> new SingleConstructorTypeContext(this :> ITypeHandler)
    static let defaultInstance = new RootTypeHandler(fun _ -> new ZeroTypeHandler() :> ITypeHandler)
    static member Default = defaultInstance :> ITypeHandler
    interface ITypeHandler with
        member this.GetKnownTypes = context.GetKnownTypes
        // TODO: cache mappings
        member this.GetMapping(targetType) = context.GetMapping(targetType)