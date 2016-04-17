using System;
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
                Paid = true,
                PaidUtc = new DateTime(2016, 1, 1),
            };
            Accounts.Add(account);
            var user = new User
            {
                Id = 1,
                Name = "Joe User",
                AccountId = 1,
                Account = account,
                Active = true
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
            schema.AddString(DateTime.Parse);
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
                .AddPostField("abc", () => GetAbcPostField())
                .AddPostField("sub", () => new Sub { Id = 1 });
            schema.AddType<Sub>().AddField(s => s.Id);
            schema.AddQuery("users", db => db.Users.AsQueryable());
            schema.AddQuery("user", new { id = 0 }, (db, args) => db.Users.AsQueryable().FirstOrDefault(u => u.Id == args.id));
        }

        private static string GetAbcPostField() => "easy as 123"; // mimic an in-memory function

        public static void InitializeAccountSchema(GraphQLSchema<MemContext> schema)
        {
            schema.AddType<Account>()
                .AddField(a => a.Id)
                .AddField(a => a.Name)
                .AddField(a => a.Paid)
                .AddField(a => a.Users)
                .AddField("activeUsers", (db, a) => a.Users.Where(u => u.Active));
            schema.AddQuery("account", new { id = 0 }, (db, args) => db.Accounts.AsQueryable().FirstOrDefault(a => a.Id == args.id));
            schema.AddQuery
                ("accountPaidBy", new { paid = default(DateTime) },
                    (db, args) => db.Accounts.AsQueryable().FirstOrDefault(a => a.PaidUtc <= args.paid));
        }
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Active { get; set; }

        public int AccountId { get; set; }
        public Account Account { get; set; }
    }

    public class Account
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Paid { get; set; }
        public DateTime? PaidUtc { get; set; }

        public List<User> Users { get; set; }
    }

    public class Sub
    {
        public int Id { get; set; }
    }
}