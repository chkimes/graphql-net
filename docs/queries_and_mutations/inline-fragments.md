# Inline Fragments

[GraphQL docs](http://graphql.org/learn/queries/#inline-fragments):

> Like many other type systems, GraphQL schemas include the ability to define interfaces and union types. Learn about them in the schema guide.
>
> If you are querying a field that returns an interface or a union type, you will need to use inline fragments to access data on the underlying concrete type.

Let's look at an example query with inline fragments:

```graphql
query Heros {
  heros {
    name
    ... on Droid {
        primaryFunction
    }
    ... on Human {
        height
    }
  }
}"
```

The expected result looks like this:

```json
{
    "heros": [
    {
        "name": "Han Solo",
        "height": 5.6430448
    },
    {
        "name": "R2-D2",
        "primaryFunction": "Astromech"
    }
    ]
}
```

The date model can be implemented as follows:

```csharp
interface ICharacter // NOTE: to support EF, this might be an abstract class
{
    int Id { get; set; }
    string Name { get; set; }
}

class Human : ICharacter
{
    public int Id { get; set; }
    public string Name { get; set; }
    public double Height { get; set; }
}

class Droid : ICharacter
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string PrimaryFunction { get; set; }
}
```

With the following context and data:

```csharp
class Context
{
    public IList<ICharacter> Heros { get; set; }
}

...

var defaultContext = new Context
{
    Heros = new List<ICharacter> {
        new Human
        {
            Id = 1,
            Name = "Han Solo",
            Height = 5.6430448
        },
        new Droid
        {
            Id = 3,
            Name = "R2-D2",
            PrimaryFunction = "Astromech"
        }
    }
};
```

The schema can be defined as follows:

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

Now we can run the query:

```csharp
var gql = new GraphQL<Context>(schema);
var queryResult = gql.ExecuteQuery(
    @"query Heros {
        heros {
            name
            ... on Droid {
                primaryFunction
            }
            ... on Human {
                height
            }
        }
    }"
    );
```

The result is as expected, see examples/08-inline-fragments for a running example.

> **Note:**
>
> Prior to version 0.3.6 class hierarchy has been automatically reflected in GraphQL types which are defined in the schema. This has been removed, since defining inheritance hierarchy in GraphQL's type system is difficult to implement \(and may not be desired\).



