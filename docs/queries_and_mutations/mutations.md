# Mutations

GraphQL introduces mutations to perform write-operations.

>Just like queries, mutations return an object type you can ask for nested fields.

The following mutation is meant to _create a review_:

```graphql
mutation CreateReviewForEpisode($ep: String!, $review: ReviewInput!) {
  createReview(episode: $ep, review: $review) {
    stars
    commentary
  }
}
```

With the following parameters:
```json
{
  "ep": "JEDI",
  "review": {
    "stars": 5,
    "commentary": "This is a great movie!"
  }
}
```

> **Note**
>
> You need GraphQL.Net version >= 3.0.1


We can define the data model and context like this:

```csharp
public class Review
{
    public string Episode { get; set; }
    public string Commentary { get; set; }
    public int Stars { get; set; }
    public int Id { get; internal set; }
}

public class ReviewInput
{
    public string Commentary { get; set; }
    public int Stars { get; set; }
}

class Context
{
    public IList<Review> Reviews { get; set; }
}
```

We create a default context with some data:
```csharp
var defaultContext = new Context
{
    Reviews = new List<Review> {
        new Review {
            Stars = 5,
            Episode = "EMPIRE",
            Commentary = "Great movie"
        }
    }
};
```

We create the schema and add `Review` as a type:

```csharp
var schema = GraphQL<Context>.CreateDefaultSchema(() => defaultContext);
schema.AddType<Review>().AddAllFields();
```

Now we add the type `ReviewInput` as a scalar to the schema so that we can use it as an input type in the mutation:

```csharp
schema.AddScalar( 
    new {
        stars = default(int),
        commentary = default(string)
    }, 
    i => new ReviewInput { Stars = i.stars, Commentary = i.commentary }, 
    "ReviewInput"
);
```

The signature of the method `AddScalar` looks like this:
```csharp
void AddScalar<TRepr, TOutput>(TRepr shape, Func<TRepr, TOutput> translate, string name = null);
```
Now we can define the mutation:
```csharp
schema.AddMutation(
    "createReview",
    new { episode = "EMPIRE", review = default(ReviewInput) },
    (db, args) =>
    {
        var newId = db.Reviews.Select(r => r.Id).Max() + 1;
        var review = new Review
        {
            Id = newId,
            Episode = args.episode,
            Commentary = args.review.Commentary,
            Stars = args.review.Stars
        };
        db.Reviews.Add(review);
        return newId;
    },
    (db, args, rId) => db.Reviews.AsQueryable().SingleOrDefault(r => r.Id == rId)
    );
```

The signature of `AddMutation` looks like this:
```csharp
GraphQLFieldBuilder<TContext, TEntity> AddMutation<TContext, TArgs, TEntity, TMutReturn>(string name, TArgs argObj, Func<TContext, TArgs, TMutReturn> mutation, Expression<Func<TContext, TArgs, TMutReturn, TEntity>> queryableGetter);
```

Finally, we complete the schema:
```csharp
schema.Complete();
```

Now we can run the mutation:
```csharp
var gql = new GraphQL<Context>(schema);
var queryResult = gql.ExecuteQuery(
    @"mutation CreateReviewForEpisode($ep: String!, $review: ReviewInput!) {
        createReview(episode: ""JEDI"", review: {commentary: ""This is a great movie!"", stars: 5}) {
            stars
            commentary
        }
    }"
);
```

> **Note**
>
>  Currently, GraphQL.Net does not support passing a separate object containing the input variable values into `ExecuteQuery`, the variables have to be put into the query string.

The result is as expected, see `examples/07-mutations` for a running example.
