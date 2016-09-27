## Integration

The files in the "Integration" folder are intended to make it easier to use this library from C# programs.

## Parsing

The files in the "Parsing" folder define the language grammar for GraphQL documents.

The goal here is to get an abstract syntax tree representation of the language, without concern for whether
it is valid in the context of a schema.

## Schema

The files in the "Schema" folder define the interfaces and types we use to represent schemas,
schema-validated syntax trees, and the generic schema validation logic.

We represents schemas with an interface `ISchema<'s>`, where `'s` can be any type the schema wants to
extend the AST with. The same type is used to extend fields, arguments, directives, etc. so it is
not completely type-safe; a schema using this extension capability will likely have to implicitly
follow rules about which subtypes of 's it puts on each AST element.

## SchemaTools

The files in the "SchemaTools" folder define code that, while technically not necessary for implementing a schema,
is expected to be useful. This includes things like mapping CLR types to GraphQL scalar types or converting an AST to
a uniform tree of selections.
