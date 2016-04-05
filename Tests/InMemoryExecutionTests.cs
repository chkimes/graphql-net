using System.Collections.Generic;
using System.Linq;
using GraphQL.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class InMemoryExecutionTests
    {
        [TestMethod]
        public void LookupSingleEntity()
        {
            var gql = MemContext.CreateDefaultContext();
            var user = (IDictionary<string, object>)gql.ExecuteQuery("query user(id:1) { id, name }")["data"];
            Assert.AreEqual(user["id"], 1);
            Assert.AreEqual(user["name"], "Joe User");
            Assert.AreEqual(user.Keys.Count, 2);
        }


        [TestMethod]
        public void AliasOneField()
        {
            var gql = MemContext.CreateDefaultContext();
            var user = (IDictionary<string, object>)gql.ExecuteQuery("query user(id:1) { idAlias : id, name }")["data"];
            Assert.IsFalse(user.ContainsKey("id"));
            Assert.AreEqual(user["idAlias"], 1);
            Assert.AreEqual(user["name"], "Joe User");
            Assert.AreEqual(user.Keys.Count, 2);
        }

        [TestMethod]
        public void NestedEntity()
        {
            var gql = MemContext.CreateDefaultContext();
            var user = (IDictionary<string, object>)gql.ExecuteQuery("query user(id:1) { id, account { id, name } }")["data"];
            Assert.AreEqual(user["id"], 1);
            Assert.AreEqual(user.Keys.Count, 2);
            Assert.IsTrue(user.ContainsKey("account"));
            var account = (IDictionary<string, object>)user["account"];
            Assert.AreEqual(account["id"], 1);
            Assert.AreEqual(account["name"], "My Test Account");
            Assert.AreEqual(account.Keys.Count, 2);
        }

        [TestMethod]
        public void AddAllFields()
        {
            var schema = GraphQL<MemContext>.CreateDefaultSchema(() => new MemContext());
            schema.AddType<User>().AddAllFields();
            schema.AddType<Account>().AddAllFields();
            schema.AddQuery("user", new { id = 0 }, (db, args) => db.Users.AsQueryable().Where(u => u.Id == args.id).FirstOrDefault());
            schema.Complete();
            var gql = new GraphQL<MemContext>(schema);
            var user = (IDictionary<string, object>)gql.ExecuteQuery("query user(id:1) { id, name }")["data"];
            Assert.AreEqual(user["id"], 1);
            Assert.AreEqual(user["name"], "Joe User");
            Assert.AreEqual(user.Keys.Count, 2);
        }

        [TestMethod]
        public void NoUserQueryReturnsNull()
        {
            var gql = MemContext.CreateDefaultContext();
            var user = (IDictionary<string, object>)gql.ExecuteQuery("query user(id:0) { id, account { id, name } }")["data"];
            Assert.IsNull(user);
        }

        [TestMethod]
        public void CustomFieldSubQuery()
        {
            var gql = MemContext.CreateDefaultContext();
            var user = (IDictionary<string, object>)gql.ExecuteQuery("query user(id:1) { id, accountPaid }")["data"];
            Assert.AreEqual(user["id"], 1);
            Assert.AreEqual(user["accountPaid"], true);
            Assert.AreEqual(user.Keys.Count, 2);
        }

        [TestMethod]
        public void CustomFieldSubQueryUsingContext()
        {
            var gql = MemContext.CreateDefaultContext();
            var user = (IDictionary<string, object>)gql.ExecuteQuery("query user(id:1) { id, total }")["data"];
            Assert.AreEqual(user["id"], 1);
            Assert.AreEqual(user["total"], 2);
            Assert.AreEqual(user.Keys.Count, 2);
        }

        [TestMethod]
        public void List()
        {
            var gql = MemContext.CreateDefaultContext();
            var users = ((List<IDictionary<string, object>>)gql.ExecuteQuery("query users { id, name }")["data"]).ToList();
            Assert.AreEqual(users.Count, 2);
            Assert.AreEqual(users[0]["id"], 1);
            Assert.AreEqual(users[0]["name"], "Joe User");
            Assert.AreEqual(users[0].Keys.Count, 2);
            Assert.AreEqual(users[1]["id"], 2);
            Assert.AreEqual(users[1]["name"], "Late Paying User");
            Assert.AreEqual(users[1].Keys.Count, 2);
        }

        [TestMethod]
        public void ListTypeIsList()
        {
            var gql = MemContext.CreateDefaultContext();
            var users = gql.ExecuteQuery("query users { id, name }")["data"];
            Assert.AreEqual(users.GetType(), typeof(List<IDictionary<string, object>>));
        }

        [TestMethod]
        public void NestedEntityList()
        {
            var gql = MemContext.CreateDefaultContext();
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

        [TestMethod]
        public void PostField()
        {
            var gql = MemContext.CreateDefaultContext();
            var user = (IDictionary<string, object>)gql.ExecuteQuery("query user(id:1) { id, abc }")["data"];
            Assert.AreEqual(user["id"], 1);
            Assert.AreEqual(user["abc"], "easy as 123");
            Assert.AreEqual(user.Keys.Count, 2);
        }

        [TestMethod]
        public void PostFieldSubQuery()
        {
            var schema = MemContext.CreateDefaultSchema();
            schema.GetType<User>().AddPostField("sub", () => new Sub {Id = 1});
            schema.AddType<Sub>().AddField(s => s.Id);
            schema.Complete();
            var gql = new GraphQL<MemContext>(schema);

            var user = (IDictionary<string, object>)gql.ExecuteQuery("query user(id:1) { sub { id } }")["data"];
            Assert.AreEqual(user.Keys.Count, 1);
            var sub = (IDictionary<string, object>)user["sub"];
            Assert.AreEqual(sub["id"], 1);
            Assert.AreEqual(sub.Keys.Count, 1);
        }

        class Sub
        {
            public int Id { get; set; }
        }
    }
}
