using System.Collections.Generic;
using System.Linq;
using GraphQL.Net;

namespace Tests
{
    public class MemContext
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
            account.Users = new List<User> { user };
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
            account2.Users = new List<User> { user2 };
        }

        public List<User> Users { get; set; } = new List<User>();
        public List<Account> Accounts { get; set; } = new List<Account>();

        public static GraphQL<MemContext> CreateDefaultContext()
        {
            var schema = CreateDefaultSchema();
            schema.Complete();
            return new GraphQL<MemContext>(schema);
        }

        public static GraphQLSchema<MemContext> CreateDefaultSchema()
        {
            var schema = GraphQL<MemContext>.CreateDefaultSchema(() => new MemContext());
            InitializeUserSchema(schema);
            InitializeAccountSchema(schema);
            return schema;
        }

        public static void InitializeUserSchema(GraphQLSchema<MemContext> schema)
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

        public static void InitializeAccountSchema(GraphQLSchema<MemContext> schema)
        {
            schema.AddType<Account>()
                .AddField(a => a.Id)
                .AddField(a => a.Name)
                .AddField(a => a.Paid)
                .AddField(a => a.Users);
            schema.AddQuery("account", new { id = 0 }, (db, args) => db.Accounts.AsQueryable().Where(a => a.Id == args.id).FirstOrDefault());
        }
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

        public List<User> Users { get; set; }
    }
}