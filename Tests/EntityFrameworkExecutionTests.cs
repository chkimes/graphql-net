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
        public static void Init(TestContext c)
        {
            using (var db = new EfContext())
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

        private static GraphQL<EfContext> CreateDefaultContext()
        {
            var schema = GraphQL<EfContext>.CreateDefaultSchema(() => new EfContext());
            InitializeUserSchema(schema);
            InitializeAccountSchema(schema);
            schema.Complete();
            return new GraphQL<EfContext>(schema);
        }

        private static void InitializeUserSchema(GraphQLSchema<EfContext> schema)
        {
            schema.AddType<User>()
                .AddField(u => u.Id)
                .AddField(u => u.Name)
                .AddField(u => u.Account)
                .AddField("total", (db, u) => db.Users.Count())
                .AddField("accountPaid", (db, u) => u.Account.Paid)
                .AddPostField("abc", () => GetAbcPostField())
                .AddPostField("sub", () => new Sub { Id = 1 });
            schema.AddType<Sub>().AddField(s => s.Id);
            schema.AddQuery("users", db => db.Users);
            schema.AddQuery("user", new { id = 0 }, (db, args) => db.Users.Where(u => u.Id == args.id).FirstOrDefault());
        }

        private static string GetAbcPostField() => "easy as 123"; // mimic an in-memory function

        private static void InitializeAccountSchema(GraphQLSchema<EfContext> schema)
        {
            schema.AddType<Account>()
                .AddField(a => a.Id)
                .AddField(a => a.Name)
                .AddField(a => a.Paid)
                .AddField(a => a.Users);
            schema.AddQuery("account", new {id = 0}, (db, args) => db.Accounts.Where(a => a.Id == args.id).FirstOrDefault());
        }

        [TestMethod] public void LookupSingleEntity() => GenericTests.LookupSingleEntity(CreateDefaultContext());
        [TestMethod] public void AliasOneField() => GenericTests.AliasOneField(CreateDefaultContext());
        [TestMethod] public void NestedEntity() => GenericTests.NestedEntity(CreateDefaultContext());
        [TestMethod] public void NoUserQueryReturnsNull() => GenericTests.NoUserQueryReturnsNull(CreateDefaultContext());
        [TestMethod] public void CustomFieldSubQuery() => GenericTests.CustomFieldSubQuery(CreateDefaultContext());
        [TestMethod] public void CustomFieldSubQueryUsingContext() => GenericTests.CustomFieldSubQueryUsingContext(CreateDefaultContext());
        [TestMethod] public void List() => GenericTests.List(CreateDefaultContext());
        [TestMethod] public void ListTypeIsList() => GenericTests.ListTypeIsList(CreateDefaultContext());
        [TestMethod] public void NestedEntityList() => GenericTests.NestedEntityList(CreateDefaultContext());
        [TestMethod] public void PostField() => GenericTests.PostField(CreateDefaultContext());
        [TestMethod] public void PostFieldSubQuery() => GenericTests.PostFieldSubQuery(CreateDefaultContext());
        [TestMethod] public void TypeName() => GenericTests.TypeName(CreateDefaultContext());

        [TestMethod]
        public void AddAllFields()
        {
            var schema = GraphQL<EfContext>.CreateDefaultSchema(() => new EfContext());
            schema.AddType<User>().AddAllFields();
            schema.AddType<Account>().AddAllFields();
            schema.AddQuery("user", new { id = 0 }, (db, args) => db.Users.Where(u => u.Id == args.id).FirstOrDefault());
            schema.Complete();
            var gql = new GraphQL<EfContext>(schema);
            var user = (IDictionary<string, object>)gql.ExecuteQuery("{ user(id:1) { id, name } }")["user"];
            Assert.AreEqual(user["id"], 1);
            Assert.AreEqual(user["name"], "Joe User");
            Assert.AreEqual(user.Keys.Count, 2);
        }

        class Sub
        {
            public int Id { get; set; }
        }

        class EfContext : DbContext
        {
            public EfContext() : base("DefaultConnection")
            {
                Database.SetInitializer(new DropCreateDatabaseIfModelChanges<EfContext>());
            }
            public IDbSet<User> Users { get; set; }
            public IDbSet<Account> Accounts { get; set; }
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
