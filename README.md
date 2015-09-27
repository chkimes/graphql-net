# GraphQL.Net
An implementation of GraphQL for .NET and IQueryable

## Description
Many of the .NET GraphQL implementations that have come out so far only seem to work in memory.
For me, this isn't terribly useful since I am almost always pulling my data out of a database (and I assume that's the case for many others). 
This library is an implementation of the GraphQL spec that converts GraphQL queries to IQueryable.
That IQueryable can then be executed using the ORM of your choice.

Here's a descriptive example, using an example from [http://facebook.github.io/graphql/#sec-Language.Query-Document.Arguments](the GraphQL spec):

```
{
  user(id: 4) {
    id
    name
    profilePic(size: 100)
  }
}
```

The above GraphQL query would be translated to:

```csharp
db.Users
    .Where(u => u.Id == 4)
    .Select(u => new
    {
        id = u.Id,
        name = u.Name,
        profilePic = db.ProfilePics.Where(p => p.UserId == u.Id && p.Size == 100)
    })
    .ToList();
```

## Building a Schema
Let's assume we have a DbContext that looks like this:

```csharp
public class TestContext : DbContext
{
    public IDbSet<User> Users { get; set; }
    public IDbSet<Account> Accounts { get; set; }
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }

    public int AccountId { get; set; }
    public Account Account { get; set; }
}

public class Account
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool Paid { get; set; }
}
```

First, we create and set the default schema by providing a function that creates our context:

```csharp
var schema = GraphQL<TestContext>.CreateDefaultSchema(() => new TestContext());
```

The default schema is required to use GraphQL<TContext>.Execute(query), but you can execute queries against the schema without it. Next, we'll define a type in the schema and properties on that type.

```csharp
schema.AddType<User>()
    .AddField(u => u.Id)
    .AddField(u => u.Name)
    .AddField(u => u.Account)
    .AddField("total", (db, u) => db.Users.Count())
    .AddField("accountPaid", (db, u) => u.Account.Paid);
```

Fields can be defined using only a property expression, or you can specify your own fields and provide a custom resolving expression. Let's do the same for account:

```csharp
schema.AddType<Account>().AddAllFields();
```

If we just want to expose all fields, we can use the `AddAllFields` helper method.

The last thing we want to do is create some queries. Let's add some to find users:

```csharp
schema.AddQuery("users", db => db.Users, list: true);

schema.AddQuery("user", new { id = 0 }, (db, args) => db.Users.Where(u => u.Id == args.id));
```

In our first query, we want to see all users so we can just return the entire list (and pass `list: true` to let the schema know a list should be returned). However, notice how in the second query we define the shape of an anonymous type `new { id = 0 }`. This is what is expected to be passed in from the GraphQL query. Since we've defined the shape, we can now use that in the `Where` clause to build our IQueryable. Now we're ready to execute a query.

## Executing Queries

```csharp
var query = @"
query user(id:1) {
    userId : id,
    userName : name,
    account {
        id
    },
    total
}";

var dict = GraphQL<TestContext>.Execute(query);
Console.WriteLine(JsonConvert.SerializeObject(dict, Formatting.Indented));

// {
//   "data": {
//     "userId": 1,
//     "userName": "Joe User",
//     "account": {
//       "id": 1
//     },
//     "total": 2
//   }
// }
```

The results from executing the query are returned as a nested Dictionary<string, object> which can easily be converted to JSON and returned to the user.

## NuGet
There isn't a NuGet package set up yet since there are a few more things required to make this useful. See the TODO section.

## TODO
Flesh out the parsing options - only field selection from queries is currently supported  
Introspection  
