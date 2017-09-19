# Interfaces

> Like many type systems, GraphQL supports interfaces. An \_Interface \_is an abstract type that includes a certain set of fields that a type must include to implement the interface.

[http://graphql.org/learn/schema/\#interfaces](http://graphql.org/learn/schema/#interfaces)

Consider the example interface `Character` from the GraphQL docs:

```graphql
interface Character {
  id: ID!
  name: String!
  friends: [Character]
  appearsIn: [Episode]!
}

type Human implements Character {
  id: ID!
  name: String!
  friends: [Character]
  appearsIn: [Episode]!
  starships: [Starship]
  totalCredits: Int
}

type Droid implements Character {
  id: ID!
  name: String!
  friends: [Character]
  appearsIn: [Episode]!
  primaryFunction: String
}
```

The interface can be added to the schema using `GraphQLSchema.AddInterfaceType`:

```csharp
var schema = GraphQL<Context>.CreateDefaultSchema(() => defaultContext);
var characterInterface = schema.AddInterfaceType<ICharacter>();
characterInterface.AddAllFields();

var humanType = schema.AddType<Human>();
humanType.AddAllFields();
humanType.AddInterface(characterInterface);

var droidType = schema.AddType<Droid>();
droidType.AddAllFields();
droidType.AddInterface(characterInterface);

schema.AddListField(
    "heros",
    db => db.Heros.AsQueryable()
    );
schema.Complete();
```

See [Inline Fragments](/docs/queries_and_mutations/inline-fragments.md) for usage with inline fragments.

