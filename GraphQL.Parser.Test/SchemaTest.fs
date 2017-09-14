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
open NUnit.Framework

// Tests that the schema resolution code works as expected with a pretend schema.
// Metadata type of our fake schema is just a string.
type FakeData = string

type NameArgument() = 
    interface ISchemaArgument<FakeData> with
        member this.ArgumentName = "name"
        member this.ArgumentType = (PrimitiveType StringType).Nullable()
        member this.Description = Some "argument for filtering by name"
        member this.Info = "Fake name arg info"

type IdArgument() = 
    interface ISchemaArgument<FakeData> with
        member this.ArgumentName = "id"
        member this.ArgumentType = (PrimitiveType IntType).NotNullable()
        member this.Description = Some "argument for filtering by id"
        member this.Info = "Fake id arg info"

type UserType() = 
    
    member private this.Field(name, fieldType : SchemaFieldType<FakeData>, args) = 
        { new ISchemaField<FakeData> with
              member __.DeclaringType = upcast this
              member __.FieldType = fieldType
              member __.FieldName = name
              member __.IsList = false
              member __.Description = Some("Description of " + name)
              member __.Info = "Info for " + name
              member __.Arguments = args |> dictionary :> _
              member __.EstimateComplexity(args) = 
                  match fieldType with
                  | ValueField _ -> Exactly(0L)
                  | QueryField _ -> 
                      if args |> Seq.exists (fun a -> a.ArgumentName = "id") then Exactly(1L)
                      else if args |> Seq.exists (fun a -> a.ArgumentName = "name") then Range(0L, 30L)
                      else Range(1L, 100L) }
    
    interface ISchemaQueryType<FakeData> with
        member this.TypeName = "User"
        member this.Description = Some "Complex user type"
        member this.Info = "Fake user type info"
        
        member this.Fields = 
            [| "id", this.Field("id", ValueField(RootTypeHandler.Default.VariableTypeOf(typeof<int>)), [||])
               "name", this.Field("name", ValueField(RootTypeHandler.Default.VariableTypeOf(typeof<string>)), [||])
               "friend", 
               this.Field("friend", QueryField(this :> ISchemaQueryType<_>), 
                          [| "name", new NameArgument() :> _
                             "id", new IdArgument() :> _ |]) |]
            |> dictionary :> _
        
        member this.PossibleTypes = Seq.empty
        member this.Interfaces = Seq.empty

type RootType() = 
    
    member private this.Field(name, fieldType : SchemaFieldType<FakeData>, args) = 
        { new ISchemaField<FakeData> with
              member __.DeclaringType = upcast this
              member __.FieldType = fieldType
              member __.FieldName = name
              member __.IsList = false
              member __.Description = Some("Description of " + name)
              member __.Info = "Info for " + name
              member __.Arguments = args |> dictionary :> _
              member __.EstimateComplexity(args) = 
                  if args |> Seq.exists (fun a -> a.ArgumentName = "id") then Exactly(1L)
                  else Range(1L, 100L) }
    
    interface ISchemaQueryType<FakeData> with
        member this.TypeName = "Root"
        member this.Description = Some "Root context type"
        member this.Info = "Fake root type info"
        
        member this.Fields = 
            [| "user", 
               this.Field("user", QueryField(new UserType()), 
                          [| "name", new NameArgument() :> _
                             "id", new IdArgument() :> _ |]) |]
            |> dictionary :> _
        
        member this.PossibleTypes = Seq.empty
        member this.Interfaces = Seq.empty

type FakeSchema() = 
    let root = new RootType() :> ISchemaQueryType<_>
    
    let types = 
        [ root
          new UserType() :> _ ]
        |> Seq.map (fun t -> t.TypeName, t)
        |> dictionary
    
    interface ISchema<FakeData> with
        member this.Directives = emptyDictionary
        member this.VariableTypes = emptyDictionary
        member this.QueryTypes = upcast types
        member this.ResolveEnumValueByName(name) = None // no enums
        member this.RootType = root

[<TestFixture>]
type SchemaTest() = 
    let schema = new FakeSchema() :> ISchema<_>
    
    let good source = 
        let doc = GraphQLDocument.Parse(schema, source)
        if doc.Operations.Count <= 0 then failwith "No operations in document!"
        for op in doc.Operations do
            printfn "Operation complexity: %A" (op.Value.EstimateComplexity())
    
    let bad reason source = 
        try 
            ignore <| GraphQLDocument.Parse(schema, source)
            failwith "Document resolved against schema when it shouldn't have!"
        with :? SourceException as ex -> 
            if (ex.Message.Contains(reason)) then ()
            else reraise()
    
    [<Test>]
    member __.TestGoodUserQuery() = good @"
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
    
    [<Test>]
    member __.TestBogusArgument() = bad "unknown argument ``occupation''" @"
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
    
    [<Test>]
    member __.TestBogusArgumentType() = bad "invalid argument ``id''" @"
{
    user(id: 1) {
        id
        name
        friend(id: ""jeremy"") {
            id
            name
        }
    }
}
"
    
    [<Test>]
    member __.TestBogusRootField() = bad "``team'' is not a field of type ``Root''" @"
{
    team {
        id
        name
    }
}
"
    
    [<Test>]
    member __.TestBogusSubField() = bad "``parent'' is not a field of type ``User''" @"
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
    
    [<Test>]
    member __.TestRecursionDepth() = bad "exceeded maximum recursion depth" @"
{
    user {
        friend(name: ""bob"") {
            friend(name: ""bob"") {
                friend(name: ""bob"") {
                    friend(name: ""bob"") {
                        friend(name: ""bob"") {
                            friend(name: ""bob"") {
                                friend(name: ""bob"") {
                                    friend(name: ""bob"") {
                                        friend(name: ""bob"") {
                                            friend(name: ""bob"") {
                                                friend(name: ""bob"") {
                                                    friend(name: ""bob"") {
                                                        friend(name: ""bob"") {
                                                            friend(name: ""bob"") {
                                                                friend(name: ""bob"") {
                                                                    friend(name: ""bob"") {
                                                                        friend(name: ""bob"") {
                                                                            friend(name: ""bob"") {
                                                                                friend(name: ""bob"") {
                                                                                    friend(name: ""bob"") {
                                                                                        friend(name: ""bob"") {
                                                                                            id
                                                                                            name
                                                                                        }
                                                                                    }
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
"
    
    [<Test>]
    member __.TestRecursionBan() = bad "fragment ``friendNamedBobForever'' is recursive" @"
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
