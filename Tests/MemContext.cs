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
            MutateMes.Add(new MutateMe
            {
                Id = 1,
                Value = 0,
            });
            account2.Users = new List<User> { user2 };
        }

        public List<User> Users { get; set; } = new List<User>();
        public List<Account> Accounts { get; set; } = new List<Account>();
        public List<MutateMe> MutateMes { get; set; } = new List<MutateMe>();
        public List<NullRef> NullRefs { get; set; } = new List<NullRef>();

        public static GraphQL<MemContext> CreateDefaultContext()
        {
            var schema = CreateDefaultSchema();
            schema.Complete();
            return new GraphQL<MemContext>(schema);
        }

        public static GraphQLSchema<MemContext> CreateDefaultSchema()
        {
            var schema = GraphQL<MemContext>.CreateDefaultSchema(() => new MemContext());
            schema.AddScalar(new { year = 0, month = 0, day = 0 }, ymd => new DateTime(ymd.year, ymd.month, ymd.day));
            InitializeUserSchema(schema);
            InitializeAccountSchema(schema);
            InitializeMutationSchema(schema);
            InitializeNullRefSchema(schema);
            return schema;
        }

        public static void InitializeUserSchema(GraphQLSchema<MemContext> schema)
        {
            var user = schema.AddType<User>();
            user.AddField(u => u.Id);
            user.AddField(u => u.Name);
            user.AddField(u => u.Account);
            user.AddField(u => u.NullRef);
            user.AddField("total", (db, u) => db.Users.Count);
            user.AddField("accountPaid", (db, u) => u.Account.Paid);
            user.AddPostField("abc", () => GetAbcPostField());
            user.AddPostField("sub", () => new Sub { Id = 1 });

            schema.AddType<Sub>().AddField(s => s.Id);
            schema.AddListField("users", db => db.Users.AsQueryable());
            schema.AddField("user", new { id = 0 }, (db, args) => db.Users.AsQueryable().FirstOrDefault(u => u.Id == args.id));
        }

        private static string GetAbcPostField() => "easy as 123"; // mimic an in-memory function

        public static void InitializeAccountSchema(GraphQLSchema<MemContext> schema)
        {
            var account = schema.AddType<Account>();
            account.AddField(a => a.Id);
            account.AddField(a => a.Name);
            account.AddField(a => a.Paid);
            account.AddListField(a => a.Users);
            account.AddListField("activeUsers", (db, a) => a.Users.Where(u => u.Active));

            schema.AddField("account", new { id = 0 }, (db, args) => db.Accounts.AsQueryable().FirstOrDefault(a => a.Id == args.id));
            schema.AddField
                ("accountPaidBy", new { paid = default(DateTime) },
                    (db, args) => db.Accounts.AsQueryable().FirstOrDefault(a => a.PaidUtc <= args.paid));
        }

        private static void InitializeMutationSchema(GraphQLSchema<MemContext> schema)
        {
            var mutate = schema.AddType<MutateMe>();
            mutate.AddAllFields();

            schema.AddField("mutateMes", new {id = 0}, (db, args) => db.MutateMes.AsQueryable().FirstOrDefault(a => a.Id == args.id));
            schema.AddMutation("mutate",
                new {id = 0, newVal = 0},
                (db, args) => db.MutateMes.AsQueryable().FirstOrDefault(a => a.Id == args.id),
                (db, args) =>
                {
                    var mutateMe = db.MutateMes.First(m => m.Id == args.id);
                    mutateMe.Value = args.newVal;
                });
        }

        private static void InitializeNullRefSchema(GraphQLSchema<MemContext> schema)
        {
            var nullRef = schema.AddType<NullRef>();
            nullRef.AddField(n => n.Id);
        }
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Active { get; set; }

        public int AccountId { get; set; }
        public Account Account { get; set; }

        public int? NullRefId { get; set; }
        public NullRef NullRef { get; set; }
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

    public class MutateMe
    {
        public int Id { get; set; }
        public int Value { get; set; }
    }

    public class NullRef
    {
        public int Id { get; set; }
    }
}