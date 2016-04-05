using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphQL.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class InMemoryExecutionTests
    {
        private static GraphQL<MemContext> CreateDefaultContext()
        {
            var schema = GraphQL<MemContext>.CreateDefaultSchema(() => new MemContext());
            InitializeUserSchema(schema);
            InitializeAccountSchema(schema);
            schema.Complete();
            return new GraphQL<MemContext>(schema);
        }

        private static void InitializeUserSchema(GraphQLSchema<MemContext> schema)
        {
            schema.AddType<User>()
                .AddField(u => u.Id)
                .AddField(u => u.Name)
                .AddField(u => u.Account)
                .AddField("total", (db, u) => db.Users.Count)
                .AddField("accountPaid", (db, u) => u.Account.Paid)
                .AddPostField("abc", () => GetAbcPostField());
            schema.AddQuery("users", db => db.Users.AsQueryable());
            schema.AddQuery("user", new { id = 0 }, (db, args) => db.Users.AsQueryable().Where(u => u.Id == args.id).FirstOrDefault());
        }

        private static string GetAbcPostField() => "easy as 123"; // mimic an in-memory function

        private static void InitializeAccountSchema(GraphQLSchema<MemContext> schema)
        {
            schema.AddType<Account>()
                .AddField(a => a.Id)
                .AddField(a => a.Name)
                .AddField(a => a.Paid)
                .AddField(a => a.Users);
            schema.AddQuery("account", new { id = 0 }, (db, args) => db.Accounts.AsQueryable().Where(a => a.Id == args.id).FirstOrDefault());
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
            var gql = CreateDefaultContext();
            var users = gql.ExecuteQuery("query users { id, name }")["data"];
            Assert.AreEqual(users.GetType(), typeof(List<IDictionary<string, object>>));
        }

        [TestMethod]
        public void NestedEntityList()
        {
            var gql = CreateDefaultContext();
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
            var gql = CreateDefaultContext();
            var user = (IDictionary<string, object>)gql.ExecuteQuery("query user(id:1) { id, abc }")["data"];
            Assert.AreEqual(user["id"], 1);
            Assert.AreEqual(user["abc"], "easy as 123");
            Assert.AreEqual(user.Keys.Count, 2);
        }

        [TestMethod]
        public void PostFieldSubQuery()
        {
            var schema = new GraphQLSchema<MemContext>(() => new MemContext());
            schema.AddType<User>().AddPostField("sub", () => new Sub {Id = 1});
            schema.AddType<Sub>().AddField(s => s.Id);
            schema.AddQuery("user", new { id = 0 }, (db, args) => db.Users.AsQueryable().Where(u => u.Id == args.id).FirstOrDefault());
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

        class MemContext
        {
            public MemContext()
            {
                var account = new Account
                {
                    Id = 1,
                    Name = "My Test Account",
                    Paid = true
                };
                Accounts.Add(account);
                var user = new User
                {
                    Id = 1,
                    Name = "Joe User",
                    AccountId = 1,
                    Account = account
                };
                Users.Add(user);
                account.Users = new List<User>{user};
                var account2 = new Account
                {
                    Id = 2,
                    Name = "Another Test Account",
                    Paid = false
                };
                Accounts.Add(account2);
                var user2 = new User
                {
                    Id = 2,
                    Name = "Late Paying User",
                    AccountId = 2,
                    Account = account2
                };
                Users.Add(user2);
                account2.Users = new List<User> {user2};
            }

            public List<User> Users { get; set; } = new List<User>();
            public List<Account> Accounts { get; set; } = new List<Account>();
        }

        class User
        {
            public int Id { get; set; }
            public string Name { get; set; }

            public int AccountId { get; set; }
            public Account Account { get; set; }
        }

        class Account
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public bool Paid { get; set; }

            public List<User> Users { get; set; }
        }
    }
}
