# Fields
Let's look at the following query:

```
{
  hero {
    name
    # Queries can have comments!
    friends {
      name
    }
  }
}
```
The expected result looks as follows:
```json
{
  "data": {
    "hero": {
      "name": "R2-D2",
      "friends": [
        {
          "name": "Luke Skywalker"
        },
        {
          "name": "Han Solo"
        },
        {
          "name": "Leia Organa"
        }
      ]
    }
  }
}
```

Assuming we have the following data models:
```csharp
 class Context
{
    public Character Hero { get; set; }
}

class Character
{
    public string Name { get; set; }
    public IEnumerable<Character> Friends { get; set; }
}
```
... and the following data:
```csharp
var context = new Context
  {
      Hero = new Character
      {
          Name = "R2-D2",
          Friends = new List<Character>
              {
                  new Character {
                    Name = "Luke Skywalker"
                  },
                  new Character {
                    Name = "Han Solo"
                  },
                  new Character {
                    Name = "Leia Organa"
                  }
              }
      }
  };
```

The GraphQL.Net schema definition could look like this:

```csharp
 var schema = GraphQL<Context>.CreateDefaultSchema(() => context);
schema.AddType<Character>().AddAllFields();
schema.AddField("hero", c => c.Hero);

schema.Complete();

var gql = new GraphQL<Context>(schema);
```

Let's run the query:
```csharp
 var queryResult = gql.ExecuteQuery(
    @"{
        hero {
            name,
            friends {
                name
            }
        }
      }"
    );
```

See `examples/01-simple-query/` for the code.


> **Note on list fields and `AddAllFields`**
> 
> `AddAllFields` does not support list fields of type array, they have to be of type `IEnumerable`.
> If we define the property `Character.Friends` as:
> ```csharp 
> public Character[] Friends { get; set; }
> ```
> We will get the following Exception on execution `gql.ExecuteQuery`:
> ```
> GraphQL.Parser.ValidationException : Unsupported CLR type ``Character''
> ```