# Union Types

> Union types are very similar to interfaces, but they don't get to specify any common fields between the types.

[http://graphql.org/learn/schema/\#union-types](http://graphql.org/learn/schema/#union-types)

Consider the example union type `SearchResult` from the GraphQL docs:

```graphql
union SearchResult = Human | Droid | Starship
```

.. and the example query:

```graphql
{
  search(text: "an") {
    ... on Human {
      name
      height
    }
    ... on Droid {
      name
      primaryFunction
    }
    ... on Starship {
      name
      length
    }
  }
}
```

The union type can be implemented using `GraphQLSchema.AddUnionType`:

```csharp
var humanType = schema.AddType<Human>();
humanType.AddAllFields();
                        
var droidType = schema.AddType<Droid>();
droidType.AddAllFields();

var starshipType = schema.AddType<Starship>();
starshipType.AddAllFields();

var searchResult = schema.AddUnionType("SearchResult", new[] {droidType.GraphQLType, humanType.GraphQLType, starshipType.GraphQLType});

schema.AddField("searchResult", new {text = ""}, (db, args) => /* Search for and return human, droid or starship. */)
      .WithReturnType(searchResult);
```



