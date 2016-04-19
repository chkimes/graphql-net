namespace GraphQL.Parser
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
    abstract member GetTranslator : targetType : System.Type -> TypeTranslator option

type IValueConverter =
    abstract member VariableTypeOf : targetType : System.Type -> VariableType
    abstract member TranslateValueTo : ty : System.Type * value : Value -> obj

type BuiltinTypeContext() =
    static let builtinTypes =
        [
            TypeTranslator.Integer(Checked.int8)
            TypeTranslator.Integer(Checked.int16)
            TypeTranslator.Integer(Checked.int32)
            TypeTranslator.Integer(Checked.int64)
            TypeTranslator.Integer(Checked.uint8)
            TypeTranslator.Integer(Checked.uint16)
            TypeTranslator.Integer(Checked.uint32)
            TypeTranslator.Integer(Checked.uint64)

            TypeTranslator.Float(single)
            TypeTranslator.Float(double)
            TypeTranslator.Float(decimal)

            TypeTranslator.String(id)

            TypeTranslator.Boolean(id)
        ] |> Seq.map (fun t -> t.CLRType, t) |> dictionary
    interface ITypeContext with
        member this.GetTranslator(targetType) = builtinTypes.TryFind(targetType)

type ChainTypeContext(primary : ITypeContext, fallback : ITypeContext) =
    interface ITypeContext with
        member this.GetTranslator(targetType) =
            match primary.GetTranslator(targetType) with
            | Some r -> Some r
            | None -> fallback.GetTranslator(targetType)

type NullableTypeContext(converter : IValueConverter) =
    interface ITypeContext with
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
            new BuiltinTypeContext()
            |??| new NullableTypeContext(this :> IValueConverter)
            |??| new ArrayTypeContext(this :> IValueConverter)
            |??| new CollectionTypeContext(this :> IValueConverter)
            |??| new EnumerableTypeContext(this :> IValueConverter)
            |??| new SingleConstructorTypeContext(this :> IValueConverter)
            |??| customContext(this :> IValueConverter)
    interface IValueConverter with
        // TODO: cache translators for CLR types
        member this.VariableTypeOf(targetType) =
            match context.GetTranslator(targetType) with
            | Some translator -> translator.VariableType
            | None -> failwith (sprintf "Unsupported CLR type ``%s''" targetType.Name)
        member this.TranslateValueTo(targetType, value) =
            match context.GetTranslator(targetType) with
            | Some translator -> translator.Translate(value)
            | None -> failwith (sprintf "Unsupported CLR type ``%s''" targetType.Name)