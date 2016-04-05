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

namespace GraphQL.Parser.Test
open GraphQL.Parser
open GraphQL.Parser.SchemaAST
open Microsoft.VisualStudio.TestTools.UnitTesting

// Tests that the schema resolution code works as expected with a pretend schema.

// Metadata type of our fake schema is just a string.
type FakeData = string

type ColorType() =
    member private this.Value(name) =
        { new ISchemaEnumValue<FakeData> with
            member __.DeclaringType = upcast this
            member __.EnumValueName = name
            member __.Description = Some ("Description of " + name)
            member __.Info = "Info for " + name
        }
    interface ISchemaQueryType<FakeData> with
        member this.TypeName = "Color"
        member this.Description = Some "Enum color type"
        member this.Info = "Fake color enum info"
        member this.Fields = [||] |> dictionary :> _
        member this.EnumValues =
            [|
               "Red", this.Value("Red")
               "Blue", this.Value("Blue")
               "Green", this.Value("Green")
            |] |> dictionary :> _

type StringType() =
    interface ISchemaQueryType<FakeData> with
        member this.TypeName = "String"
        member this.Description = Some "Primitive string type"
        member this.Info = "Fake string type info"
        member this.Fields = [||] |> dictionary :> _
        member this.EnumValues = [||] |> dictionary :> _

type IntegerType() =
    interface ISchemaQueryType<FakeData> with
        member this.TypeName = "Integer"
        member this.Description = Some "Primitive integer type"
        member this.Info = "Fake integer type info"
        member this.Fields = [||] |> dictionary :> _
        member this.EnumValues = [||] |> dictionary :> _

type NameArgument() =
    interface ISchemaArgument<FakeData> with
        member this.ArgumentName = "name"
        member this.Description = Some "argument for filtering by name"
        member this.Info = "Fake name arg info"
        member this.ValidateValue(value) =
            let arg =
                lazy { new ISchemaArgumentValue<FakeData> with
                    member __.Argument = this :> _
                    member __.Info = "fake arg value info"
                    member __.Value = value
                }
            match value with
            | PrimitiveValue (StringPrimitive str) -> Valid arg.Value
            | VariableRefValue def ->
                match def.VariableType.Type with
                | NamedType (:? StringType as st) -> Valid arg.Value
                | _ -> Invalid "Variable of non-string type"
            | _ -> Invalid "Literal of non-string type"

type IdArgument() =
    interface ISchemaArgument<FakeData> with
        member this.ArgumentName = "id"
        member this.Description = Some "argument for filtering by id"
        member this.Info = "Fake id arg info"
        member this.ValidateValue(value) =
            let arg =
                lazy { new ISchemaArgumentValue<FakeData> with
                    member __.Argument = this :> _
                    member __.Info = "fake arg value info"
                    member __.Value = value
                }
            match value with
            | PrimitiveValue (IntPrimitive _) -> Valid arg.Value
            | VariableRefValue def ->
                match def.VariableType.Type with
                | NamedType (:? IntegerType as st) -> Valid arg.Value
                | _ -> Invalid "Variable of non-integer type"
            | _ -> Invalid "Literal of non-integer type"

type UserType() =
    member private this.Field(name, fieldType : ISchemaQueryType<FakeData>, args) =
        { new ISchemaField<FakeData> with
            member __.DeclaringType = upcast this
            member __.FieldType = fieldType
            member __.FieldName = name
            member __.Description = Some ("Description of " + name)
            member __.Info = "Info for " + name
            member __.Arguments = args |> dictionary :> _
        }
    interface ISchemaQueryType<FakeData> with
        member this.TypeName = "User"
        member this.Description = Some "Complex user type"
        member this.Info = "Fake user type info"
        member this.EnumValues = [||] |> dictionary :> _
        member this.Fields =
            [|
                "id", this.Field("id", new IntegerType(), [||])
                "name", this.Field("name", new StringType(), [||])
                "friend", this.Field("friend", this,
                    [|
                        "name", new NameArgument() :> _
                        "id", new IdArgument() :> _
                    |])
                "favoriteColor", this.Field("favoriteColor", new ColorType(), [||])
            |] |> dictionary :> _

type RootType() =
    member private this.Field(name, fieldType : ISchemaQueryType<FakeData>, args) =
        { new ISchemaField<FakeData> with
            member __.DeclaringType = upcast this
            member __.FieldType = fieldType
            member __.FieldName = name
            member __.Description = Some ("Description of " + name)
            member __.Info = "Info for " + name
            member __.Arguments = args |> dictionary :> _
        }
    interface ISchemaQueryType<FakeData> with
        member this.TypeName = "Root"
        member this.Description = Some "Root context type"
        member this.Info = "Fake root type info"
        member this.EnumValues = [||] |> dictionary :> _
        member this.Fields =
            [|
                "user", this.Field("user", new UserType(),
                    [|
                        "name", new NameArgument() :> _
                        "id", new IdArgument() :> _
                    |])
            |] |> dictionary :> _

type FakeSchema() =
    let root = new RootType() :> ISchemaQueryType<_>
    let types =
        [
            root
            new ColorType() :> _
            new UserType() :> _
        ]
    interface ISchema<FakeData> with
        member this.ResolveDirectiveByName(name) = None // no directives
        member this.ResolveEnumValueByName(name) =
            types |> List.tryPick (fun ty -> ty.EnumValues.TryFind(name))
        member this.ResolveQueryTypeByName(name) =
            types |> List.tryFind (fun ty -> ty.TypeName = name)
        member this.RootType = root
        

[<TestClass>]
type SchemaTest() =
    let schema = new FakeSchema() :> ISchema<_>
    let good source =
        let doc = GraphQLDocument.Parse(schema, source)
        if doc.Operations.Count <= 0 then
            failwith "No operations in document!"
    let bad reason source =
        try
            ignore <| GraphQLDocument.Parse(schema, source)
            failwith "Document resolved against schema when it shouldn't have!"
        with
        | :? ValidationException as ex ->
            if (ex.Message.Contains(reason)) then ()
            else reraise()
    [<TestMethod>]
    member __.TestGoodUserQuery() =
        good @"
{
    user(id: 1) {
        id
        name
        friend(name: ""bob"") {
            id
            name
        }
    }
}
"

    [<TestMethod>]
    member __.TestBogusArgument() =
        bad "unknown argument ``occupation''" @"
{
    user(id: 1) {
        id
        name
        friend(occupation: ""welder"") {
            id
            name
        }
    }
}
"

    [<TestMethod>]
    member __.TestBogusRootField() =
        bad "``team'' is not a field of type ``Root''" @"
{
    team {
        id
        name
    }
}
"

    [<TestMethod>]
    member __.TestBogusSubField() =
        bad "``parent'' is not a field of type ``User''" @"
{
    user {
        id
        name
        parent {
            id
            name
        }
    }
}
"

    [<TestMethod>]
    member __.TestRecursionDepth() =
        bad "exceeded maximum recursion depth" @"
fragment friendNamedBobForever on User {
    friend(name: ""bob"") {
        ...friendNamedBobForever
    }
}
{
    user {
        ...friendNamedBobForever
    }
}
"