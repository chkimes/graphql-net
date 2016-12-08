# Build a Schema

Let's assume we have an Entity Framework DbContext that looks like this:

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

The default schema is required to use the helper method`GraphQL<TContext>.Execute(query)`, but you can execute queries against the schema without it. Next, we'll define a type in the schema and fields on that type.

```csharp
var user = schema.AddType<User>();
user.AddField(u => u.Id);
user.AddField(u => u.Name);
user.AddField(u => u.Account);
user.AddField("totalUsers", (db, u) => db.Users.Count());
user.AddField("accountPaid", (db, u) => u.Account.Paid);
```

Fields can be defined using only a property expression, or you can specify your own fields and provide a custom resolving expression. Let's do the same for account:

```csharp
schema.AddType<Account>().AddAllFields();
```

If we just want to expose all fields, we can use the `AddAllFields` helper method.

The last thing we want to do is create some queries, as fields on the schema itself. Let's add some to find users:

```csharp
schema.AddListField("users", db => db.Users);
schema.AddField("user", new { id = 0 }, (db, args) => db.Users.Where(u => u.Id == args.id).FirstOrDefault());
```

In our first query, we want to see all users so we can just return the entire list. However, notice how in the second query we define the shape of an anonymous type `new { id = 0 }`. This is what is expected to be passed in from the GraphQL query. Since we've defined the shape, we can now use that in the `Where` clause to build our IQueryable. We use `FirstOrDefault` to signify that this query will return a single result.

```csharp
schema.Complete();
```

Finally, we complete the schema when we've finished setting up.  Now we're ready to execute a query.