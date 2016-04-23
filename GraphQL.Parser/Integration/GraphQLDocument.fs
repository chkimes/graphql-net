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
open GraphQL.Parser.SchemaResolver
open System.Collections.Generic

type GraphQLParserDocument(source : string, document : ParserAST.Document) =
    member __.Source = source
    member __.AST = document
    static member Parse(source) =
        let document = Parser.parseDocument source
        new GraphQLParserDocument(source, document)

type GraphQLDocument<'s>(schema : ISchema<'s>, source : string, operations : Operation<'s> ListWithSource) =
    member __.Schema = schema
    member __.Source = source
    member __.Operations = operations
    static member Parse(schema, source) =
        let doc = GraphQLParserDocument.Parse(source)
        let context = new DocumentContext<'s>(schema, doc.AST)
        let ops = context.ResolveOperations()
        new GraphQLDocument<'s>(schema, source, ops)
