using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using GraphQL.Net;
using NUnit.Framework;
using SQLite.CodeFirst;

namespace Tests
{
    [TestFixture]
    public class EntityFrameworkExecutionTests
    {
        [OneTimeSetUp]
        public static void Init()
        {
            using (var db = new EfContext())
            {
                if (db.Accounts.Any())
                    return;

                var account = new Account
                {
                    Name = "My Test Account",
                    Paid = true,
                    PaidUtc = new DateTime(2016, 1, 1),
                };
                db.Accounts.Add(account);
                var user = new User
                {
                    Name = "Joe User",
                    Account = account,
                    Active = true,
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
            schema.AddString(DateTime.Parse);
            InitializeUserSchema(schema);
            InitializeAccountSchema(schema);
            schema.Complete();
            return new GraphQL<EfContext>(schema);
        }

        private static void InitializeUserSchema(GraphQLSchema<EfContext> schema)
        {
            var user = schema.AddType<User>();
            user.AddField(u => u.Id);
            user.AddField(u => u.Name);
            user.AddField(u => u.Account);
            user.AddField("total", (db, u) => db.Users.Count());
            user.AddField("accountPaid", (db, u) => u.Account.Paid);
            user.AddPostField("abc", () => GetAbcPostField());
            user.AddPostField("sub", () => new Sub { Id = 1 });

            schema.AddType<Sub>().AddField(s => s.Id);
            schema.AddQuery("users", db => db.Users);
            schema.AddQuery("user", new { id = 0 }, (db, args) => db.Users.FirstOrDefault(u => u.Id == args.id));
        }

        private static string GetAbcPostField() => "easy as 123"; // mimic an in-memory function

        private static void InitializeAccountSchema(GraphQLSchema<EfContext> schema)
        {
            var account = schema.AddType<Account>();
            account.AddField(a => a.Id);
            account.AddField(a => a.Name);
            account.AddField(a => a.Paid);
            account.AddListField(a => a.Users);
            account.AddListField("activeUsers", (db, a) => a.Users.Where(u => u.Active));

            schema.AddQuery("account", new {id = 0}, (db, args) => db.Accounts.FirstOrDefault(a => a.Id == args.id));
            schema.AddQuery
                ("accountPaidBy", new { paid = default(DateTime) },
                    (db, args) => db.Accounts.AsQueryable().FirstOrDefault(a => a.PaidUtc <= args.paid));
        }

        [Test] public void LookupSingleEntity() => GenericTests.LookupSingleEntity(CreateDefaultContext());
        [Test] public void AliasOneField() => GenericTests.AliasOneField(CreateDefaultContext());
        [Test] public void NestedEntity() => GenericTests.NestedEntity(CreateDefaultContext());
        [Test] public void NoUserQueryReturnsNull() => GenericTests.NoUserQueryReturnsNull(CreateDefaultContext());
        [Test] public void CustomFieldSubQuery() => GenericTests.CustomFieldSubQuery(CreateDefaultContext());
        [Test] public void CustomFieldSubQueryUsingContext() => GenericTests.CustomFieldSubQueryUsingContext(CreateDefaultContext());
        [Test] public void List() => GenericTests.List(CreateDefaultContext());
        [Test] public void ListTypeIsList() => GenericTests.ListTypeIsList(CreateDefaultContext());
        [Test] public void NestedEntityList() => GenericTests.NestedEntityList(CreateDefaultContext());
        [Test] public void PostField() => GenericTests.PostField(CreateDefaultContext());
        [Test] public void PostFieldSubQuery() => GenericTests.PostFieldSubQuery(CreateDefaultContext());
        [Test] public void TypeName() => GenericTests.TypeName(CreateDefaultContext());
        [Test] public void DateTimeFilter() => GenericTests.DateTimeFilter(CreateDefaultContext());
        [Test] public void EnumerableSubField() => GenericTests.EnumerableSubField(CreateDefaultContext());

        [Test]
        public void AddAllFields()
        {
            var schema = GraphQL<EfContext>.CreateDefaultSchema(() => new EfContext());
            schema.AddType<User>().AddAllFields();
            schema.AddType<Account>().AddAllFields();
            schema.AddQuery("user", new { id = 0 }, (db, args) => db.Users.FirstOrDefault(u => u.Id == args.id));
            schema.Complete();

            var gql = new GraphQL<EfContext>(schema);
            var results = gql.ExecuteQuery("{ user(id:1) { id, name } }");
            Test.DeepEquals(results, "{ user: { id: 1, name: 'Joe User' } }");
        }

        class Sub
        {
            public int Id { get; set; }
        }

        class EfContext : DbContext
        {
            public EfContext() : base("DefaultConnection") { }

            protected override void OnModelCreating(DbModelBuilder modelBuilder)
            {
                Database.SetInitializer(new SqliteDropCreateDatabaseWhenModelChanges<EfContext>(modelBuilder));
                base.OnModelCreating(modelBuilder);
            }

            public IDbSet<User> Users { get; set; }
            public IDbSet<Account> Accounts { get; set; }
        }

        class User
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public bool Active { get; set; }

            public int AccountId { get; set; }
            public Account Account { get; set; }
        }

        class Account
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public bool Paid { get; set; }
            public DateTime? PaidUtc { get; set; }

            public List<User> Users { get; set; }
        }
    }
}
