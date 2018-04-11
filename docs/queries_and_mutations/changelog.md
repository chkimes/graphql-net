# Changelog

## 0.3.6

* **Breaking Changes:**

  * Automatic reflect class hierarchy in GraphQL types which have been added to the schema has been removed. Use `GraphQLSchema.AddInterfaceType` and `GraphQLSchema.AddUnionType` to migrate your schema if necessary.

* **Additional API:**

  * `GraphQLSchema.AddInterfaceType` :   See documentation [Interfaces](/schema-and-types/interfaces.md).

  * `GraphQLSchema.AddUnionType`:  See documentation [Union types](/schema-and-types/union-types.md).

* **Improvements:**

  * Better support of introspection.
  * Fix issue related to fields of type list of scalar

For details see PR \#83 \([https://github.com/ckimes89/graphql-net/pull/83](https://github.com/ckimes89/graphql-net/pull/83)\).

