# Fragments

With _fragments_ GraphQL provides a concept of reusable units constructing a set of fields and include them in queries where you need them.

Here is an example using a fragment to avoid repetition:

```graphql
{
  leftComparison: hero(episode: EMPIRE) {
    ...comparisonFields
  }
  rightComparison: hero(episode: JEDI) {
    ...comparisonFields
  }
}

fragment comparisonFields on Character {
  name
  appearsIn
  friends {
    name
  }
}
```

The result could look like this:
```json
{
  "data": {
    "leftComparison": {
      "name": "Luke Skywalker",
      "appearsIn": [
        "NEWHOPE",
        "EMPIRE",
        "JEDI"
      ],
      "friends": [
        {
          "name": "Han Solo"
        },
        {
          "name": "Leia Organa"
        },
        {
          "name": "C-3PO"
        },
        {
          "name": "R2-D2"
        }
      ]
    },
    "rightComparison": {
      "name": "R2-D2",
      "appearsIn": [
        "NEWHOPE",
        "EMPIRE",
        "JEDI"
      ],
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

The data models could like this:

```csharp
class Context
{
    public IList<Character> Heros { get; set; }
}

public class Character
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Episode { get; set; }
    public string[] AppearsIn { get; set; }
    public IEnumerable<Character> Friends { get; set; }
}
```

The schema and context could look like this:
```csharp
var schema = GraphQL<Context>.CreateDefaultSchema(() =>
  new Context
  {
      Heros = new List<Character> {
          new Character {
              Id = "1000",
              Name = "Luke Skywalker",
              Episode = "EMPIRE",
              AppearsIn = new string[] { "NEWHOPE", "EMPIRE", "JEDI"},
              Friends = new List<Character> {
                  new Character { Name = "Han Solo"},
                  new Character { Name = "Leia Organa"},
                  new Character { Name = "C-3PO"},
                  new Character { Name = "R2-D2"}
              }
          },
          new Character {
              Id = "1001",
              Name = "R2-D2",
              Episode = "JEDI",
              AppearsIn = new string[] {"NEWHOPE", "EMPIRE", "JEDI" },
              Friends = new List<Character> {
                  new Character { Name = "Luke Skywalker"},
                  new Character { Name = "Han Solo"},
                  new Character { Name = "Leia Organa"}
              }
          }
      }
  });
schema.AddType<Character>().AddAllFields();
schema.AddField(
    "hero",
    new { episode = "EMPIRE" },
    (c, args) => c.Heros.SingleOrDefault(h => h.Episode == args.episode));

schema.Complete();
```

Now we run the query:
```csharp
var gql = new GraphQL<Context>(schema);
var queryResult = gql.ExecuteQuery(
  @"{
        leftComparison: hero(episode: ""EMPIRE"") {
          ...comparisonFields
        }
        rightComparison: hero(episode: ""JEDI"") {
          ...comparisonFields
        }
    }
    fragment comparisonFields on Character {
      name
      appearsIn
      friends {
          name
      }
    }"
  );
```
The result is as expected, see `examples/04-fragments` for a running example.