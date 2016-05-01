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

// This module implements the introspection schema described in
// section 4.2 of the GraphQL spec.

type TypeKind =
    | SCALAR = 1
    | OBJECT = 2
    | INTERFACE = 3
    | UNION = 4
    | ENUM = 5
    | INPUT_OBJECT = 6
    | LIST = 7
    | NON_NULL = 8

type DirectiveLocation =
    | QUERY = 1
    | MUTATION = 2
    | FIELD = 3
    | FRAGMENT_DEFINITION = 4
    | FRAGMENT_SPREAD = 5
    | INLINE_FRAGMENT = 6

type IntroType =
    {
        Kind: TypeKind
        Name : string option
        Description : string option
        // OBJECT and INTERFACE only
        Fields : IntroField seq option
        // OBJECT only
        Interfaces : IntroType seq option
        // INTERFACE and UNION only
        PossibleTypes : IntroType seq option
        // ENUM only
        EnumValues : IntroEnumValue seq option
        // INPUT_OBJECT only
        InputFields : IntroInputValue seq option
        // NON_NULL and LIST only
        OfType : IntroType option
    }
    static member Default =
        {
            Kind = TypeKind.SCALAR
            Name = None
            Description = None
            Fields = None
            Interfaces = None
            PossibleTypes = None
            EnumValues = None
            InputFields = None
            OfType = None
        }
    static member Of(coreType : CoreVariableType) =
        match coreType with
        | PrimitiveType _ -> IntroType.Default
        | NamedType namedType ->
            { IntroType.Of(namedType.CoreType) with
                Name = Some namedType.TypeName
            }
        | EnumType enumType ->
            { IntroType.Default with
                Kind = TypeKind.ENUM
                Name = Some enumType.EnumName
                EnumValues = enumType.Values.Values |> Seq.map IntroEnumValue.Of |> Some
            }
        | ListType elementType ->
            { IntroType.Default with
                Kind = TypeKind.LIST
                OfType = IntroType.Of(elementType) |> Some
            }
        | ObjectType fieldTypes ->
            { IntroType.Default with
                Kind = TypeKind.INPUT_OBJECT
                InputFields = fieldTypes |> Seq.map IntroInputValue.Of |> Some
            }
    static member Of(varType : VariableType) =
        if not varType.Nullable then
            { IntroType.Default with
                Kind = TypeKind.NON_NULL
                OfType = IntroType.Of(varType.Type) |> Some
            }
        else IntroType.Of(varType.Type)
    static member Of(queryType : ISchemaQueryType<'s>) =
        let fields = queryType.Fields.Values |> Seq.map IntroField.Of
        { IntroType.Default with
            Kind = TypeKind.OBJECT
            Name = Some queryType.TypeName
            Description = queryType.Description
            Fields = fields |> Some
            Interfaces = Some Seq.empty
        }
    static member Of(fieldType : SchemaFieldType<'s>) =
        match fieldType with
        | QueryField qty -> IntroType.Of(qty)
        | ValueField vty -> IntroType.Of(vty)
        
and IntroField =
    {
        Name : string
        Description : string option
        Args : IntroInputValue seq
        Type : IntroType
        IsDeprecated : bool
        DeprecationReason : string option
    }
    static member Of(field : ISchemaField<'s>) =
        let args = field.Arguments.Values |> Seq.map IntroInputValue.Of
        let ty = IntroType.Of(field.FieldType)
        {
            Name = field.FieldName
            Description = field.Description
            Args = args
            Type = ty
            IsDeprecated = false
            DeprecationReason = None
        }
and IntroInputValue =
    {
        Name : string
        Description : string option
        Type : IntroType
        DefaultValue : string option // wat?
    }
    static member Of(KeyValue(fieldName, fieldType)) =
        {
            Name = fieldName
            Description = None
            Type = IntroType.Of(fieldType)
            DefaultValue = None
        }
    static member Of(arg : ISchemaArgument<'s>) =
        {
            Name = arg.ArgumentName
            Description = arg.Description
            Type = IntroType.Of(arg.ArgumentType)
            DefaultValue = None
        }
and IntroEnumValue =
    {
        Name : string
        Description: string option
        IsDeprecated : bool
        DeprecationReason : string option
    }
    static member Of(enumValue : EnumTypeValue) =
        {
            Name = enumValue.ValueName
            Description = None
            IsDeprecated = false
            DeprecationReason = None
        }
        

type IntroDirective =
    {
        Name : string
        Description : string option
        Locations : DirectiveLocation seq
        Args : IntroInputValue seq
    }
    static member Of(dir : ISchemaDirective<'s>) =
        {
            Name = dir.DirectiveName
            Description = dir.Description
            Locations = // TODO: let the schema directive say what locations it's valid in
                [ DirectiveLocation.QUERY
                  DirectiveLocation.MUTATION
                  DirectiveLocation.FIELD
                  DirectiveLocation.FRAGMENT_DEFINITION
                  DirectiveLocation.FRAGMENT_SPREAD
                  DirectiveLocation.INLINE_FRAGMENT ]
            Args = dir.Arguments.Values |> Seq.map IntroInputValue.Of
        }

type IntroSchema =
    {
        Types : IntroType seq
        QueryType : IntroType
        MutationType : IntroType option
        Directives : IntroDirective seq
    }
    static member Of(schema : ISchema<'s>) =
        {
            Types =
                [
                    schema.VariableTypes.Values |> Seq.map IntroType.Of
                    schema.QueryTypes.Values |> Seq.map IntroType.Of
                ] |> Seq.concat
            QueryType = IntroType.Of(schema.RootType)
            MutationType = None // TODO: support mutation schema
            Directives =
                schema.Directives.Values |> Seq.map IntroDirective.Of
        }
