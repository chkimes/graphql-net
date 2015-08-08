# EntityFramework.GraphQL
Query an Entity Framework model with GraphQL

## Usage
For a DbContext named MyContext with a User model in the database, add queries to the Schema like so:

```csharp
GraphQL<MyContext>.Schema.CreateQuery("users", db => db.Users, list: true);
GraphQL<MyContext>.Schema.CreateQuery("user", new { id = 0 },
    (db, args) => db.Users.Where(u => u.Id == args.id));
```

The first query returns all users in the database and has no arguments to supply. The second query returns a specific user, given an id in the arguments. To call these methods using GraphQL, use the static `Execute` method:

```csharp
var queryUsers = @"
  query users {
    id
    name
  }";

var users = GraphQL<MyContext>.Execute(queryUsers);
Console.WriteLine(JsonConvert.SerializeObject(users));

var queryUser = @"
  query user(id: 1) {
    id
    jeffsName : name
    account {
        id
    }
  }";

var user = GraphQL<MyContext>.Execute(queryUser);
Console.WriteLine(JsonConvert.SerializeObject(user));
```

The `Execute` method returns a nested `Dictionary<string, object>`, so it's mostly just useful for serializing to JSON.

Multiple users output:
```json
{
  "data": {
    "users": [
      {
        "id": 1,
        "name": "Jeff"
      },
      {
        "id": 2,
        "name": "Joe"
      }
    ]
  }
}
[{ "id": 1, "name": "Test User" }]
```

Single user output:
```json
{
  "data": {
    "user": {
      "id": 1,
      "jeffsName": "Jeff",
      "account": {
        "id": 1000
      }
    }
  }
}
```

This should be fairly simple to hook into a Web API endpoint.

## NuGet
There isn't a NuGet package set up yet since it is only marginally useful at the moment. See the TODO section.

## TODO
Flesh out the parsing options - only field selection from queries is currently supported  
Custom resolvers  
Introspection  
