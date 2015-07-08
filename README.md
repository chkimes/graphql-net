# EntityFramework.GraphQL
Query an Entity Framework model with GraphQL

## Usage
For a DbContext named MyContext with a User model in the database:
```csharp
var queryStr = @"
  query user {
    id
    name
  }";
var parser = new Parser();
var query = parser.Parse(queryStr);

var executor = new Executor<MyContext>();
var obj = executor.Execute(query);
Console.WriteLine(JsonConvert.SerializeObject(obj));
```

Output:
```json
[{ "id": 1, "name": "Test User" }]
```

This should be fairly simple to hook into a Web API endpoint.

## NuGet
There isn't a NuGet package set up yet since it is only marginally useful at the moment. See the TODO section.

## TODO
Query Parameterization - Hard to make useful queries without this at the moment  
Flesh out the parsing options - only field selection from queries is currently supported  
Custom resolvers  
Introspection  
