using System.Collections.Generic;
using GraphQL.Net;
using NUnit.Framework;

namespace Tests
{
    public static class GenericTests
    {
        public static void LookupSingleEntity<TContext>(GraphQL<TContext> gql)
        {
            var user = (IDictionary<string, object>)gql.ExecuteQuery("{ user(id:1) { id, name } }")["user"];
            Assert.AreEqual(user["id"], 1);
            Assert.AreEqual(user["name"], "Joe User");
            Assert.AreEqual(user.Keys.Count, 2);
        }

        public static void AliasOneField<TContext>(GraphQL<TContext> gql)
        {
            var user = (IDictionary<string, object>)gql.ExecuteQuery("{ user(id:1) { idAlias : id, name } }")["user"];
            Assert.IsFalse(user.ContainsKey("id"));
            Assert.AreEqual(user["idAlias"], 1);
            Assert.AreEqual(user["name"], "Joe User");
            Assert.AreEqual(user.Keys.Count, 2);
        }

        public static void NestedEntity<TContext>(GraphQL<TContext> gql)
        {
            var user = (IDictionary<string, object>)gql.ExecuteQuery("{ user(id:1) { id, account { id, name } } }")["user"];
            Assert.AreEqual(user["id"], 1);
            Assert.AreEqual(user.Keys.Count, 2);
            Assert.IsTrue(user.ContainsKey("account"));
            var account = (IDictionary<string, object>)user["account"];
            Assert.AreEqual(account["id"], 1);
            Assert.AreEqual(account["name"], "My Test Account");
            Assert.AreEqual(account.Keys.Count, 2);
        }

        public static void NoUserQueryReturnsNull<TContext>(GraphQL<TContext> gql)
        {
            var user = (IDictionary<string, object>)gql.ExecuteQuery("{ user(id:0) { id, account { id, name } } }")["user"];
            Assert.IsNull(user);
        }

        public static void CustomFieldSubQuery<TContext>(GraphQL<TContext> gql)
        {
            var user = (IDictionary<string, object>)gql.ExecuteQuery("{ user(id:1) { id, accountPaid } }")["user"];
            Assert.AreEqual(user["id"], 1);
            Assert.AreEqual(user["accountPaid"], true);
            Assert.AreEqual(user.Keys.Count, 2);
        }

        public static void CustomFieldSubQueryUsingContext<TContext>(GraphQL<TContext> gql)
        {
            var user = (IDictionary<string, object>)gql.ExecuteQuery("{ user(id:1) { id, total } }")["user"];
            Assert.AreEqual(user["id"], 1);
            Assert.AreEqual(user["total"], 2);
            Assert.AreEqual(user.Keys.Count, 2);
        }

        public static void List<TContext>(GraphQL<TContext> gql)
        {
            var users = (List<IDictionary<string, object>>)gql.ExecuteQuery("{ users { id, name } }")["users"];
            Assert.AreEqual(users.Count, 2);
            Assert.AreEqual(users[0]["id"], 1);
            Assert.AreEqual(users[0]["name"], "Joe User");
            Assert.AreEqual(users[0].Keys.Count, 2);
            Assert.AreEqual(users[1]["id"], 2);
            Assert.AreEqual(users[1]["name"], "Late Paying User");
            Assert.AreEqual(users[1].Keys.Count, 2);
        }

        public static void ListTypeIsList<TContext>(GraphQL<TContext> gql)
        {
            var users = gql.ExecuteQuery("{ users { id, name } }")["users"];
            Assert.AreEqual(users.GetType(), typeof(List<IDictionary<string, object>>));
        }

        public static void NestedEntityList<TContext>(GraphQL<TContext> gql)
        {
            var account = (IDictionary<string, object>)gql.ExecuteQuery("{ account(id:1) { id, users { id, name } } }")["account"];
            Assert.AreEqual(account["id"], 1);
            Assert.AreEqual(account.Keys.Count, 2);
            Assert.IsTrue(account.ContainsKey("users"));
            var users = (List<IDictionary<string, object>>)account["users"];
            Assert.AreEqual(users.Count, 1);
            Assert.AreEqual(users[0]["id"], 1);
            Assert.AreEqual(users[0]["name"], "Joe User");
            Assert.AreEqual(users[0].Keys.Count, 2);
        }

        public static void PostField<TContext>(GraphQL<TContext> gql)
        {
            var user = (IDictionary<string, object>)gql.ExecuteQuery("{ user(id:1) { id, abc } }")["user"];
            Assert.AreEqual(user["id"], 1);
            Assert.AreEqual(user["abc"], "easy as 123");
            Assert.AreEqual(user.Keys.Count, 2);
        }

        public static void PostFieldSubQuery<TContext>(GraphQL<TContext> gql)
        {
            var user = (IDictionary<string, object>)gql.ExecuteQuery("{ user(id:1) { sub { id } } }")["user"];
            Assert.AreEqual(user.Keys.Count, 1);
            var sub = (IDictionary<string, object>)user["sub"];
            Assert.AreEqual(sub["id"], 1);
            Assert.AreEqual(sub.Keys.Count, 1);
        }

        public static void TypeName<TContext>(GraphQL<TContext> gql)
        {
            var user = (IDictionary<string, object>)gql.ExecuteQuery("{ user(id:1) { id, __typename } }")["user"];
            Assert.AreEqual(user["id"], 1);
            Assert.AreEqual(user["__typename"], "User");
            Assert.AreEqual(user.Keys.Count, 2);
        }

        public static void EnumerableSubField<TContext>(GraphQL<TContext> gql)
        {
            var account = (IDictionary<string, object>) gql.ExecuteQuery("{ account(id:1) { activeUsers { id, name } } }")["account"];
            Assert.AreEqual(account.Keys.Count, 1);
            var activeUsers = (List<IDictionary<string, object>>) account["activeUsers"];
            Assert.AreEqual(activeUsers.Count, 1);
            Assert.AreEqual(activeUsers[0]["id"], 1);
            Assert.AreEqual(activeUsers[0]["name"], "Joe User");
            Assert.AreEqual(activeUsers[0].Keys.Count, 2);

            var account2 = (IDictionary<string, object>) gql.ExecuteQuery("{ account(id:2) { activeUsers { id, name } } }")["account"];
            Assert.AreEqual(account2.Keys.Count, 1);
            var activeUsers2 = (List<IDictionary<string, object>>) account2["activeUsers"];
            Assert.AreEqual(activeUsers2.Count, 0);
        }
    }
}
