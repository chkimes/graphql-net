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

module GraphQL.Parser.Introspection

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
        Name : string
        Description : string
        // OBJECT and INTERFACE only
        Fields : IntroField seq option
        // ENUM only
        EnumValues : IntroEnumValue seq option
        // INPUT_OBJECT only
        InputFields : IntroInputValue seq option
        // NON_NULL and LIST only
        OfType : IntroType option
    }
and IntroField =
    {
        Name : string
        Description : string
        Args : IntroInputValue seq
        Type : IntroType
        IsDeprecated : bool
        DeprecationReason : string
    }
and IntroInputValue =
    {
        Name : string
        Description : string
        Type : IntroType
        DefaultValue : string // string? wat?
    }
and IntroEnumValue =
    {
        Name : string
        Description: string
        IsDeprecated : bool
        DeprecationReason : string
    }

type IntroDirective =
    {
        Name : string
        Description : string
        Locations : DirectiveLocation seq
        Args : IntroInputValue seq
    }

type IntroSchema =
    {
        Types : IntroType seq
        QueryType : IntroType
        MutationType : IntroType option
        Directives : IntroDirective seq
    }
