#Inline Fragments

[GraphQL docs](http://graphql.org/learn/queries/#inline-fragments):
>Like many other type systems, GraphQL schemas include the ability to define interfaces and union types. Learn about them in the schema guide.

>If you are querying a field that returns an interface or a union type, you will need to use inline fragments to access data on the underlying concrete type.

Let's look at an example query with inline fragments:

```graphql
query Heros {
  heros {
    name
    ... on Droid {
        primaryFunction
    }
    ... on Stormtrooper {
        specialization
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
        "name": "FN-2187",
        "specialization": "Imperial Snowtrooper",
        "height": 4.9
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
class Character
{
    public int Id { get; set; }
    public string Name { get; set; }
}

class Human : Character
{
    public double Height { get; set; }
}
class Stormtrooper : Human
{
    public string Specialization { get; set; }
}

class Droid : Character
{
    public string PrimaryFunction { get; set; }
}
```

With the following context and data:
```csharp
class Context
{
    public IList<Character> Heros { get; set; }
}

...

var defaultContext = new Context
{
    Heros = new List<Character> {
        new Human
        {
            Id = 1,
            Name = "Han Solo",
            Height = 5.6430448
        },
        new Stormtrooper
        {
            Id = 2,
            Name = "FN-2187",
            Height = 4.9,
            Specialization = "Imperial Snowtrooper"
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
schema.AddType<Character>().AddAllFields();
schema.AddType<Human>().AddAllFields();
schema.AddType<Stormtrooper>().AddAllFields();
schema.AddType<Droid>().AddAllFields();
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
            ... on Stormtrooper {
                specialization
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
> If two types with a common base type (which may be `object`) both have a property with the same name but with different types, the schema builder will raise an error:
> ```
>at GraphQL.Net.GraphQLSchema`1.CompleteType(GraphQLType type)
   at GraphQL.Net.GraphQLSchema`1.CompleteTypes(IEnumerable`1 types)
   at GraphQL.Net.GraphQLSchema`1.Complete()
   at _08_inline_fragments.InlineFragmentsExample.RunExample() 
Result Message:	System.ArgumentException : The type 'Character' has multiple fields named 'test' with different types.
>```
> This may be supported in the future.
> As a workaround the properties can be added to the grapqhl types using different field names.
