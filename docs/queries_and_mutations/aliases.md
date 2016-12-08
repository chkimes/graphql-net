# Aliases

Result objects can be renamed using aliases:

```csharp
{
  empireHero: hero(episode: EMPIRE) {
    name
  }
  jediHero: hero(episode: JEDI) {
    name
  }
}
```
Result:
```json
{
  "data": {
    "empireHero": {
      "name": "Luke Skywalker"
    },
    "jediHero": {
      "name": "R2-D2"
    }
  }
}
```

This can be implemented like this:

```csharp
class Context
{
    public IList<Hero> Heros { get; set; }
}

class Hero
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Episode { get; set; }
}

...

var schema = GraphQL<Context>.CreateDefaultSchema(() =>
  new Context
  {
      Heros = new List<Hero> {
          new Hero { Id = "1000", Name = "Luke Skywalker", Episode = "EMPIRE" },
          new Hero { Id = "1001", Name = "R2-D2", Episode = "JEDI" }
      }
  });
schema.AddType<Hero>().AddAllFields();
schema.AddField(
    "hero",
    new { episode = "EMPIRE" },
    (c, args) => c.Heros.SingleOrDefault(h => h.Episode == args.episode));

schema.Complete();

var gql = new GraphQL<Context>(schema);
var queryResult = gql.ExecuteQuery("{empireHero: hero(episode: \"EMPIRE\") {name}, jediHero: hero(episode: \"JEDI\") {name}}");
```

See `examples/03-aliases`.