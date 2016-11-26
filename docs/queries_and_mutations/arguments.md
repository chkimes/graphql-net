# Arguments

Grapqhl-net supports field arguments.

The query:
```csharp
{
  human(id: "1000") {
    name
    height
  }
}
```
Should return:
```json
{
  "data": {
    "human": {
      "name": "Luke Skywalker",
      "height": 1.72
    }
  }
}
```

This can be implemented like this:

```csharp
class Context
{
    public IList<Human> Humans { get; set; }
}

class Human
{
    public string Id { get; set; }
    public string Name { get; set; }
    public double Height { get; set; }
}

...

var schema = GraphQL<Context>.CreateDefaultSchema(() =>
new Context
{
    Humans = new List<Human> {
        new Human { Id = "1000", Name = "Luke Skywalker", Height = 1.72 }
    }
});
schema.AddType<Human>().AddAllFields();
schema.AddField(
    "human",
    new { id = "-1" },
    (c, args) => c.Humans.SingleOrDefault(h => h.Id == args.id));

schema.Complete();

var gql = new GraphQL<Context>(schema);
var queryResult = gql.ExecuteQuery("{human(id: \"1000\") {name, height}}");
```
Arguments can be specified on scalar fields as well:
```csharp
{
  human(id: "1000") {
    name
    height(unit: FOOT)
  }
}
```
The result should look like this:
```json
{
  "data": {
    "human": {
      "name": "Luke Skywalker",
      "height": 5.6430448
    }
  }
}
```

This can be implemented like this:

```csharp
var schema = GraphQL<Context>.CreateDefaultSchema(() =>
  new Context
  {
      Humans = new List<Human> {
          new Human { Id = "1000", Name = "Luke Skywalker", Height = 1.72 }
      }
  });
var humanSchema = schema.AddType<Human>();
humanSchema.AddField(h => h.Id);
humanSchema.AddField(h => h.Name);
humanSchema.AddField(
    "height", 
    new { unit = "METER"}, 
    (c, args, h) => args.unit == "FOOT" ? h.Height * 3.28084 : h.Height);

schema.AddField(
    "human",
    new { id = "-1" },
    (c, args) => c.Humans.SingleOrDefault(h => h.Id == args.id));

schema.Complete();

var gql = new GraphQL<Context>(schema);
var queryResult = gql.ExecuteQuery("{human(id: \"1000\") {name, height(unit: \"FOOT\")}}");
```

See `examples/02-field-argumens`.