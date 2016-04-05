using System.Collections.Generic;
using System.Linq;
using GraphQL.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    static class GenericTests
    {
        public static void LookupSingleEntity<TContext>(GraphQL<TContext> gql)
        {
            var user = (IDictionary<string, object>)gql.ExecuteQuery("query user(id:1) { id, name }")["data"];
            Assert.AreEqual(user["id"], 1);
            Assert.AreEqual(user["name"], "Joe User");
            Assert.AreEqual(user.Keys.Count, 2);
        }

        public static void AliasOneField<TContext>(GraphQL<TContext> gql)
        {
            var user = (IDictionary<string, object>)gql.ExecuteQuery("query user(id:1) { idAlias : id, name }")["data"];
            Assert.IsFalse(user.ContainsKey("id"));
            Assert.AreEqual(user["idAlias"], 1);
            Assert.AreEqual(user["name"], "Joe User");
            Assert.AreEqual(user.Keys.Count, 2);
        }

        public static void NestedEntity<TContext>(GraphQL<TContext> gql)
        {
            var user = (IDictionary<string, object>)gql.ExecuteQuery("query user(id:1) { id, account { id, name } }")["data"];
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
            var user = (IDictionary<string, object>)gql.ExecuteQuery("query user(id:0) { id, account { id, name } }")["data"];
            Assert.IsNull(user);
        }

        public static void CustomFieldSubQuery<TContext>(GraphQL<TContext> gql)
        {
            var user = (IDictionary<string, object>)gql.ExecuteQuery("query user(id:1) { id, accountPaid }")["data"];
            Assert.AreEqual(user["id"], 1);
            Assert.AreEqual(user["accountPaid"], true);
            Assert.AreEqual(user.Keys.Count, 2);
        }

        public static void CustomFieldSubQueryUsingContext<TContext>(GraphQL<TContext> gql)
        {
            var user = (IDictionary<string, object>)gql.ExecuteQuery("query user(id:1) { id, total }")["data"];
            Assert.AreEqual(user["id"], 1);
            Assert.AreEqual(user["total"], 2);
            Assert.AreEqual(user.Keys.Count, 2);
        }

        public static void List<TContext>(GraphQL<TContext> gql)
        {
            var users = ((List<IDictionary<string, object>>)gql.ExecuteQuery("query users { id, name }")["data"]).ToList();
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
            var users = gql.ExecuteQuery("query users { id, name }")["data"];
            Assert.AreEqual(users.GetType(), typeof(List<IDictionary<string, object>>));
        }

        public static void NestedEntityList<TContext>(GraphQL<TContext> gql)
        {
            var account = (IDictionary<string, object>)gql.ExecuteQuery("query account(id:1) { id, users { id, name } }")["data"];
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
            var user = (IDictionary<string, object>)gql.ExecuteQuery("query user(id:1) { id, abc }")["data"];
            Assert.AreEqual(user["id"], 1);
            Assert.AreEqual(user["abc"], "easy as 123");
            Assert.AreEqual(user.Keys.Count, 2);
        }

        public static void PostFieldSubQuery<TContext>(GraphQL<TContext> gql)
        {
            var user = (IDictionary<string, object>)gql.ExecuteQuery("query user(id:1) { sub { id } }")["data"];
            Assert.AreEqual(user.Keys.Count, 1);
            var sub = (IDictionary<string, object>)user["sub"];
            Assert.AreEqual(sub["id"], 1);
            Assert.AreEqual(sub.Keys.Count, 1);
        }
    }
}
