# Introduction

Many of the .NET GraphQL implementations that have come out so far only seem to work in memory.
For me, this isn't terribly useful since most of my data is stored in a database (and I assume that's the case for many others). 
This library is an implementation of the GraphQL spec that converts GraphQL queries to IQueryable.
That IQueryable can then be executed using the ORM of your choice.

Here's a descriptive example, using an example from [the GraphQL spec](http://facebook.github.io/graphql/#sec-Language.Query-Document.Arguments):

```
{
  user(id: 4) {
    id
    name
    profilePic(size: 100)
  }
}
```

The above GraphQL query could be translated to:

```csharp
db.Users
  .Where(u => u.Id == 4)
  .Select(u => new
  {
      id = u.Id,
      name = u.Name,
      profilePic = db.ProfilePics
                     .FirstOrDefault(p => p.UserId == u.Id && p.Size == 100)
                     .Url
  })
  .FirstOrDefault();
```