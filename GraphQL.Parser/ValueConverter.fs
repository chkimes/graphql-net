namespace GraphQL.Parser
open System
open System.Reflection
open System.Collections.Generic

type TypeTranslator(clrType : System.Type, varType : VariableType, translate : Value -> obj) =
    member this.CLRType = clrType
    member this.VariableType = varType
    member this.Translate = translate
    static member Integer<'a>(translate : int64 -> 'a) =
        new TypeTranslator(typeof<'a>, (PrimitiveType IntType).NotNullable()
            , fun (v : Value) -> v.GetInteger() |> translate |> box)
    static member Float<'a>(translate : double -> 'a) =
        new TypeTranslator(typeof<'a>, (PrimitiveType FloatType).NotNullable()
            , fun (v : Value) -> v.GetFloat() |> translate |> box)
    static member String<'a>(translate : string -> 'a) =
        new TypeTranslator(typeof<'a>, (PrimitiveType StringType).NotNullable()
            , fun (v : Value) -> v.GetString() |> translate |> box)
    static member Boolean<'a>(translate : bool -> 'a) =
        new TypeTranslator(typeof<'a>, (PrimitiveType BooleanType).NotNullable()
            , fun (v : Value) -> v.GetBoolean() |> translate |> box)

type ITypeContext =
    abstract member GetNamedTypes : CoreVariableType seq
    abstract member GetTranslator : targetType : System.Type -> TypeTranslator option

type IValueConverter =
    abstract member GetNamedTypes : CoreVariableType seq
    abstract member VariableTypeOf : targetType : System.Type -> VariableType
    abstract member TranslateValueTo : ty : System.Type * value : Value -> obj

type BuiltinTypeContext() =
    static let integer (convert : int64 -> 'a) name (min, max) =
        new TypeTranslator
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
        new TypeTranslator
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

            TypeTranslator.String(id)

            TypeTranslator.Boolean(id)
        ] |> Seq.map (fun t -> t.CLRType, t) |> dictionary
    interface ITypeContext with
        member this.GetNamedTypes =
            builtinTypes.Values
            |> Seq.map (fun t -> t.VariableType.Type)
        member this.GetTranslator(targetType) = builtinTypes.TryFind(targetType)

type ChainTypeContext(primary : ITypeContext, fallback : ITypeContext) =
    interface ITypeContext with
        member this.GetNamedTypes = Seq.append primary.GetNamedTypes fallback.GetNamedTypes
        member this.GetTranslator(targetType) =
            match primary.GetTranslator(targetType) with
            | Some r -> Some r
            | None -> fallback.GetTranslator(targetType)

type NullableTypeContext(converter : IValueConverter) =
    interface ITypeContext with
        member this.GetNamedTypes = upcast []
        member this.GetTranslator(ty) =
            if ty.IsValueType
                && ty.IsGenericType
                && ty.GetGenericTypeDefinition() = typedefof<System.Nullable<_>>
            then
                let clrElementType = ty.GetGenericArguments().[0]
                let elementType = converter.VariableTypeOf(clrElementType).Type
                new TypeTranslator
                    ( ty
                    , new VariableType(elementType, isNullable = true)
                    , fun value ->
                        if value = NullValue then null
                        else
                            let wrappedValue = converter.TranslateValueTo(clrElementType, value)
                            ty.GetConstructor([|clrElementType|]).Invoke([|wrappedValue|])
                    ) |> Some
            else None

type ArrayTypeContext(converter : IValueConverter) =
    let translateArray clrElementType value =
        match value with
        | NullValue -> null
        | ListValue vs ->
            let arr = System.Array.CreateInstance(clrElementType, [|vs.Count|])
            let mutable i = 0
            for { Value = v } in vs do
                arr.SetValue(converter.TranslateValueTo(clrElementType, v), i)
                i <- i + 1
            box arr
        | _ -> failwith "Value not suitable for array initialization"
    interface ITypeContext with
        member this.GetNamedTypes = upcast []
        member this.GetTranslator(targetType) =
            if targetType.IsArray then
                let clrElementType = targetType.GetElementType()
                let elementType = converter.VariableTypeOf(clrElementType)
                new TypeTranslator
                    ( targetType
                    , (ListType elementType).Nullable()
                    , translateArray clrElementType
                    ) |> Some
            else None

type CollectionTypeContext(converter : IValueConverter) =
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
                ignore <| add.Invoke(collection, [|converter.TranslateValueTo(clrElementType, v)|])
            collection
        | _ -> failwith "Value not suitable for collection initialization"
    interface ITypeContext with
        member this.GetNamedTypes = upcast []
        member this.GetTranslator(targetType) =
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
                let elementType = converter.VariableTypeOf(clrElementType)
                new TypeTranslator
                    ( targetType
                    , (ListType elementType).Nullable(not targetType.IsValueType)
                    , translateCollection emptyCons icollection clrElementType
                    ) |> Some

type EnumerableTypeContext(converter : IValueConverter) =
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
                ignore <| collectionAdd.Invoke(collection, [|converter.TranslateValueTo(clrElementType, v)|])
            cons.Invoke([|collection|])
        | _ -> failwith "Value not suitable for IEnumerable initialization"
    interface ITypeContext with
        member this.GetNamedTypes = upcast []
        member this.GetTranslator(targetType) =
            let interfaces = targetType.GetInterfaces()
            let ienumerable =
                interfaces
                |> Array.tryFind(fun i ->
                    i.IsGenericType && i.GetGenericTypeDefinition() = typedefof<IEnumerable<_>>)
            match ienumerable with
            | None -> None
            | Some ienumerable ->
                let clrElementType = ienumerable.GetGenericArguments().[0]
                let elementType = converter.VariableTypeOf(clrElementType)
                let cons = targetType.GetConstructor([|ienumerable|]) // can we be constructed from another IEnumerable<T>?
                if isNull cons then None else
                new TypeTranslator
                    ( targetType
                    , (ListType elementType).Nullable(not targetType.IsValueType)
                    , translateEnumerable cons clrElementType
                    ) |> Some

type SingleConstructorTypeContext(converter : IValueConverter) =
    interface ITypeContext with
        member this.GetNamedTypes = upcast []
        member this.GetTranslator(targetType) =
            let constructors = targetType.GetConstructors()
            if constructors.Length <> 1 then None else
            let cons = constructors.[0]
            let parameters = cons.GetParameters()
            let fields =
                [
                    for parameter in parameters do
                        let varTy = converter.VariableTypeOf(parameter.ParameterType)
                        yield parameter.Name, varTy
                ] |> dictionary
            new TypeTranslator
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
                            let clrValue = converter.TranslateValueTo(parameter.ParameterType, value)
                            arguments.[i] <- clrValue
                        cons.Invoke(arguments)
                    | _ -> failwith "Invalid object fields for constructor"
                ) |> Some

module private TypeContextUtilities =
    let (|??|) (context1 : #ITypeContext) (context2 : #ITypeContext) =
        new ChainTypeContext(context1, context2) :> ITypeContext
open TypeContextUtilities

type ValueConverter(customContext : IValueConverter -> ITypeContext) as this =
    let mutable context : ITypeContext = Unchecked.defaultof<ITypeContext>
    do
        context <-
            customContext(this :> IValueConverter) // custom types get first precedence
            |??| new BuiltinTypeContext()
            |??| new NullableTypeContext(this :> IValueConverter)
            |??| new ArrayTypeContext(this :> IValueConverter)
            |??| new CollectionTypeContext(this :> IValueConverter)
            |??| new EnumerableTypeContext(this :> IValueConverter)
            |??| new SingleConstructorTypeContext(this :> IValueConverter)
    interface IValueConverter with
        member this.GetNamedTypes = context.GetNamedTypes
        // TODO: cache translators for CLR types
        member this.VariableTypeOf(targetType) =
            match context.GetTranslator(targetType) with
            | Some translator -> translator.VariableType
            | None -> failwith (sprintf "Unsupported CLR type ``%s''" targetType.Name)
        member this.TranslateValueTo(targetType, value) =
            match context.GetTranslator(targetType) with
            | Some translator -> translator.Translate(value)
            | None -> failwith (sprintf "Unsupported CLR type ``%s''" targetType.Name)