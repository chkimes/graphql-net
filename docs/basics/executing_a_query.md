# Executing a Query
```csharp
var query = @"{
user(id:1) {
    userId : id
    userName : name
    account {
        id
        paid
    }
    totalUsers
}";

var gql = new GraphQL<TestContext>(schema);
var dict = gql.ExecuteQuery(query);
Console.WriteLine(JsonConvert.SerializeObject(dict, Formatting.Indented));

// {
//   "user": {
//     "userId": 1,
//     "userName": "Joe User",
//     "account": {
//       "id": 1,
//       "paid": true
//     },
//     "totalUsers": 2
//   }
// }
```

The results from executing the query are returned as a nested Dictionary<string, object> which can easily be converted to JSON and returned to the user.