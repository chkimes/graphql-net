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

module GraphQL.Parser.ParserAST
open System.Collections.Generic

// This file defines the types that make up the AST parsed from a GraphQL document
// before any validation is applied. These types describe the structure of the document
// but there is no guarantee that variable names used as values are declared somewhere
// or that type names are valid in any given schema.

// Most of the time when we need to put source info on an AST element
// it's because it's within a list, therefore the SourceInfo of its containing
// element will be too general to direct the user to the problem location.
// Because of how common this is, we define the type alias `ListWithSource` for a list of
// elements with source info on each one.
type ListWithSource<'a> = ResizeArray<'a WithSource>

/// String alias for a variable name.
/// Variable names in the AST do *not* contain a leading "$".
type VariableName = string

/// String alias for a field name in an object literal or selection.
/// May be any identifier.
type FieldName = string

/// Represents a GraphQL value.
/// This may be a variable name, a literal, or a list/object
/// containing more values.
type Value =
    | Variable of VariableName
    | IntValue of int64
    | FloatValue of double
    | StringValue of string
    | BooleanValue of bool
    | EnumValue of string
    | ListValue of Value ListWithSource
    | ObjectValue of IDictionary<FieldName, Value WithSource>

/// String alias for an argument name, which may be any identifier.
type ArgumentName = string
/// String alias for a directive name, which may be any identifier.
type DirectiveName = string
/// String alias for a fragment name, which may be any identifier except `on`.
type FragmentName = string
/// String alias for a type name.
type TypeName = string
/// String alias for an operation name.
type OperationName = string

/// Represents an `argName: argValue` pair passed as an argument
/// to a directive or field.
/// See arguments in section 2.6 of the GraphQL spec.
type Argument =
    {
        ArgumentName : ArgumentName
        ArgumentValue : Value
    }

/// Represents a directive, e.g. `@dirName(arg1: val1, arg2: val2)`.
type Directive =
    {
        DirectiveName : DirectiveName
        Arguments : Argument ListWithSource
    }

/// Represents a usage of a named fragment via the spread operator `...fragmentName`.
type FragmentSpread =
    {
        FragmentName : FragmentName
        Directives : Directive ListWithSource
    }

/// Represents a field within a selection, e.g. `user(id: 10) { name, email }`.
/// The field may have arguments, directives, and sub-selections.
/// See fields in section 2.5 of the GraphQL spec.
type Field =
    {
        /// If an alias is supplied, the response object will return data
        /// in a field with this name instead of `FieldName`.
        /// This is useful when the same field name is used with different arguments.
        Alias : FieldName option
        FieldName : FieldName
        Arguments : Argument ListWithSource
        Directives : Directive ListWithSource
        Selections : Selection ListWithSource
    }
/// Represents a single selection element.
/// This may be a field, a fragment spread, or an inline fragment.
/// See selection sets in section 2.4 of the GraphQL spec.
and Selection =
    | FieldSelection of Field
    | FragmentSpreadSelection of FragmentSpread
    | InlineFragmentSelection of InlineFragment
/// Represents an inline fragment, which is used to apply a type condition
/// or additional directives to some fields of a selection set.
/// See inline fragments in section 2.8.2 of the GraphQL spec.
and InlineFragment =
    {
        /// Only include these selections in the result for objects
        /// of the named type. This feature only makes sense when
        /// the GraphQL server can return objects of different runtime types
        /// from the same query (perhaps different subclasses of the same base).
        TypeCondition : TypeName option
        Directives : Directive ListWithSource
        Selections : Selection ListWithSource
    }

/// Represents a named type or list of (possibly nullable) types.
/// This is separate from `TypeDescription` in order to make it impossible
/// to describe a "nullable (not nullable (type))" or similar nonsense.
type CoreTypeDescription =
    | NamedType of TypeName
    | ListType of TypeDescription
/// Represents a GraphQL type.
/// This may be a named type or a list of types and may be nullable.
/// See types in section 3 of the GraphQL spec.
and TypeDescription =
    {
        Type : CoreTypeDescription
        Nullable : bool
    }

/// Represents the declaration of a variable, e.g. `$myVar: Int = 0`.
/// See variables in section 2.10 of the GraphQL spec.
type VariableDefinition =
    {
        /// The name of the variable being declared.
        /// Note that it does *not* contain a leading "$".
        VariableName : VariableName
        Type : TypeDescription
        /// If present, a default value for the variable.
        DefaultValue : Value option
    }

/// Distinguishes between mutation and query operation types.
type OperationType =
    | Query
    | Mutation

/// Represents a mutation or query operation in long-hand form, which
/// means it can be named, can have variable definitions, and can
/// have directives applied to it.
type LonghandOperation =
    {
        Type : OperationType
        Name : OperationName option
        VariableDefinitions : VariableDefinition ListWithSource
        Directives : Directive ListWithSource
        Selections : Selection ListWithSource
    }

/// Represents an operation.
/// See operations in section 2.3 of the GraphQL spec.
type Operation =
    /// An anonymous shorthand query consisting only of a selection list.
    | ShorthandOperation of Selection ListWithSource
    /// A query or mutation, possibly with directives, variables, etc.
    | LonghandOperation of LonghandOperation

/// Represents the declaration of a fragment.
/// A fragment is a set of selections on a type that can be reused by name in
/// the document. It may also have directives applied to the selections.
/// See fragments in section 2.8 of the GraphQL spec.
type Fragment =
    {
        /// The name of the fragment.
        FragmentName : FragmentName
        /// The type name these selections are intended to be made on.
        TypeCondition : TypeName
        Directives : Directive ListWithSource
        Selections : Selection ListWithSource
    }

/// Represents the declaration of either an operation or a named fragment.
/// A GraphQL document is just a list of these.
type Definition =
    | OperationDefinition of Operation
    | FragmentDefinition of Fragment

/// Represents a complete GraphQL document.
type Document =
    {
        Definitions : Definition ListWithSource
    }