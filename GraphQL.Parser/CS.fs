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

namespace GraphQL.Parser.CS
open GraphQL.Parser
open System.Collections.Generic
open System.Runtime.CompilerServices

// This module adds abstract classes suitable as a starting point for implementing
// a schema from a C# project.

[<AbstractClass>]
type SchemaCS<'s>() =
    abstract member ResolveVariableType : name : string -> ISchemaVariableType
    default this.ResolveVariableType(_) = Unchecked.defaultof<_>
    abstract member ResolveQueryType : name : string -> ISchemaQueryType<'s>
    abstract member ResolveEnumValue : name : string -> EnumValue
    default this.ResolveEnumValue(_) = Unchecked.defaultof<_>
    abstract member ResolveDirective : name : string -> ISchemaDirective<'s>
    default this.ResolveDirective(_) = Unchecked.defaultof<_>
    abstract member RootType : ISchemaQueryType<'s>
    interface ISchema<'s> with
        member this.ResolveDirectiveByName(name) =
            this.ResolveDirective(name) |> obj2option
        member this.ResolveVariableTypeByName(name) =
            this.ResolveVariableType(name) |> obj2option
        member this.ResolveQueryTypeByName(name) =
            this.ResolveQueryType(name) |> obj2option
        member this.ResolveEnumValueByName(name) =
            this.ResolveEnumValue(name) |> obj2option
        member this.RootType = this.RootType

[<AbstractClass>]
type SchemaQueryTypeCS<'s>() =
    abstract member TypeName : string
    abstract member Description : string
    default this.Description = null
    abstract member Info : 's
    default this.Info = Unchecked.defaultof<'s>
    abstract member Fields : IReadOnlyDictionary<string, ISchemaField<'s>>
    interface ISchemaQueryType<'s> with
        member this.TypeName = this.TypeName
        member this.Description = this.Description |> obj2option
        member this.Info = this.Info
        member this.Fields = this.Fields

[<AbstractClass>]
type SchemaFieldCS<'s>() =
    abstract member DeclaringType : ISchemaQueryType<'s>
    abstract member FieldType : SchemaFieldType<'s>
    abstract member FieldName : string
    abstract member Description : string
    default this.Description = null
    abstract member Info : 's
    default this.Info = Unchecked.defaultof<'s>
    abstract member Arguments : IReadOnlyDictionary<string, ISchemaArgument<'s>>
    default this.Arguments = emptyDictionary
    abstract member EstimateComplexity : ISchemaArgument<'s> seq -> Complexity
    default this.EstimateComplexity(_) = Complexity.One
    interface ISchemaField<'s> with
        member this.DeclaringType = this.DeclaringType
        member this.FieldType = this.FieldType
        member this.FieldName = this.FieldName
        member this.Description = this.Description |> obj2option
        member this.Info = this.Info
        member this.Arguments = this.Arguments
        member this.EstimateComplexity(args) = this.EstimateComplexity(args)

[<AbstractClass>]
type SchemaQueryableFieldCS<'s>() =
    inherit SchemaFieldCS<'s>()
    override this.FieldType = QueryField this.QueryableFieldType
    abstract member QueryableFieldType : ISchemaQueryType<'s>

[<AbstractClass>]
type SchemaValueFieldCS<'s>() =
    inherit SchemaFieldCS<'s>()
    override this.FieldType = ValueField (new VariableType(this.ValueFieldType, this.IsNullable))
    abstract member IsNullable : bool
    default this.IsNullable = false
    abstract member ValueFieldType : CoreVariableType

[<Extension>]
type CSExtensions =
    [<Extension>]
    static member Values<'a>(elements : 'a WithSource seq) = elements |> Seq.map (fun e -> e.Value)

        
