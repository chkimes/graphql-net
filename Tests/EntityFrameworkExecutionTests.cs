using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using GraphQL.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class EntityFrameworkExecutionTests
    {
        [ClassInitialize]
        public static void Init(Microsoft.VisualStudio.TestTools.UnitTesting.TestContext c)
        {
            using (var db = new TestContext())
            {
                if (db.Accounts.Any())
                    return;

                var account = new Account
                {
                    Name = "My Test Account",
                    Paid = true
                };
                db.Accounts.Add(account);
                var user = new User
                {
                    Name = "Joe User",
                    Account = account
                };
                db.Users.Add(user);
                var account2 = new Account
                {
                    Name = "Another Test Account",
                    Paid = false
                };
                db.Accounts.Add(account2);
                var user2 = new User
                {
                    Name = "Late Paying User",
                    Account = account2
                };
                db.Users.Add(user2);
                db.SaveChanges();
            }
        }

        private static GraphQL<TestContext> CreateDefaultContext()
        {
            var schema = GraphQL<TestContext>.CreateDefaultSchema(() => new TestContext());
            InitializeUserSchema(schema);
            InitializeAccountSchema(schema);
            return new GraphQL<TestContext>(schema);
        }

        private static void InitializeUserSchema(GraphQLSchema<TestContext> schema)
        {
            schema.AddType<User>()
                .AddField(u => u.Id)
                .AddField(u => u.Name)
                .AddField(u => u.Account)
                .AddField("total", (db, u) => db.Users.Count())
                .AddField("accountPaid", (db, u) => u.Account.Paid);
            schema.AddQuery("users", db => db.Users);
            schema.AddQuery("user", new { id = 0 }, (db, args) => db.Users.Where(u => u.Id == args.id).FirstOrDefault());
        }

        private static void InitializeAccountSchema(GraphQLSchema<TestContext> schema)
        {
            schema.AddType<Account>()
                .AddField(a => a.Id)
                .AddField(a => a.Name)
                .AddField(a => a.Paid);
        }

        [TestMethod]
        public void LookupSingleEntity()
        {
            var gql = CreateDefaultContext();
            var user = (IDictionary<string, object>)gql.ExecuteQuery("query user(id:1) { id, name }")["data"];
            Assert.AreEqual(user["id"], 1);
            Assert.AreEqual(user["name"], "Joe User");
            Assert.AreEqual(user.Keys.Count, 2);
        }

        [TestMethod]
        public void AliasOneField()
        {
            var gql = CreateDefaultContext();
            var user = (IDictionary<string, object>)gql.ExecuteQuery("query user(id:1) { idAlias : id, name }")["data"];
            Assert.IsFalse(user.ContainsKey("id"));
            Assert.AreEqual(user["idAlias"], 1);
            Assert.AreEqual(user["name"], "Joe User");
            Assert.AreEqual(user.Keys.Count, 2);
        }

        [TestMethod]
        public void NestedEntity()
        {
            var gql = CreateDefaultContext();
            var user = (IDictionary<string, object>)gql.ExecuteQuery("query user(id:1) { id, account { id, name } }")["data"];
            Assert.AreEqual(user["id"], 1);
            Assert.AreEqual(user.Keys.Count, 2);
            Assert.IsTrue(user.ContainsKey("account"));
            var account = (IDictionary<string, object>) user["account"];
            Assert.AreEqual(account["id"], 1);
            Assert.AreEqual(account["name"], "My Test Account");
            Assert.AreEqual(account.Keys.Count, 2);
        }

        [TestMethod]
        public void AddAllFields()
        {
            var schema = GraphQL<TestContext>.CreateDefaultSchema(() => new TestContext());
            schema.AddType<User>().AddAllFields();
            schema.AddQuery("user", new { id = 0 }, (db, args) => db.Users.Where(u => u.Id == args.id).FirstOrDefault());
            var gql = new GraphQL<TestContext>(schema);
            var user = (IDictionary<string, object>)gql.ExecuteQuery("query user(id:1) { id, name }")["data"];
            Assert.AreEqual(user["id"], 1);
            Assert.AreEqual(user["name"], "Joe User");
            Assert.AreEqual(user.Keys.Count, 2);
        }

        [TestMethod]
        public void NoUserQueryReturnsNull()
        {
            var gql = CreateDefaultContext();
            var user = (IDictionary<string, object>)gql.ExecuteQuery("query user(id:0) { id, account { id, name } }")["data"];
            Assert.IsNull(user);
        }

        [TestMethod]
        public void CustomFieldSubQuery()
        {
            var gql = CreateDefaultContext();
            var user = (IDictionary<string, object>)gql.ExecuteQuery("query user(id:1) { id, accountPaid }")["data"];
            Assert.AreEqual(user["id"], 1);
            Assert.AreEqual(user["accountPaid"], true);
            Assert.AreEqual(user.Keys.Count, 2);
        }

        [TestMethod]
        public void CustomFieldSubQueryUsingContext()
        {
            var gql = CreateDefaultContext();
            var user = (IDictionary<string, object>)gql.ExecuteQuery("query user(id:1) { id, total }")["data"];
            Assert.AreEqual(user["id"], 1);
            Assert.AreEqual(user["total"], 2);
            Assert.AreEqual(user.Keys.Count, 2);
        }

        [TestMethod]
        public void List()
        {
            var gql = CreateDefaultContext();
            var users = ((IEnumerable<IDictionary<string, object>>)gql.ExecuteQuery("query users { id, name }")["data"]).ToList();
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
            var gql = CreateDefaultContext();
            var users = gql.ExecuteQuery("query users { id, name }")["data"];
            Assert.AreEqual(users.GetType(), typeof(List<IDictionary<string, object>>));
        }
    }

    public class TestContext : DbContext
    {
        public TestContext() : base("DefaultConnection")
        {
            Database.SetInitializer(new DropCreateDatabaseIfModelChanges<TestContext>());
        }
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
}
