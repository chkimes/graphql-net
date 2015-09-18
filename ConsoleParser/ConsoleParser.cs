using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using GraphQL.Net;
using Newtonsoft.Json;

namespace ConsoleParser
{
    class ConsoleParser
    {
        static void Main()
        {
            // these should be type methods
            ScratchResolvers.AddResolver<TestContext, User>("test", (db, u) => db.Users.Where(us => us.Id == u.Id).ToList());
            ScratchResolvers.AddResolver<TestContext, User>("test2", (db, u) => u.Name);

            // this should be a schema method
            ScratchResolvers.Context<TestContext>
                .UsingParameters(new { id = 0 })
                .AddQuery<User>("users", args => db => db.Users.Where(u => u.Id == args.id));

            // ideal, but C# compiler doesn't like it
            ScratchResolvers.Context<TestContext>.AddQuery("user", new { id = 0 }, args => db => db.Users.Where(u => u.Id == args.id));
            ScratchResolvers.Context<TestContext>.AddQuery("user", new { id = 0 }, args => (Expression<Func<TestContext, IQueryable<User>>>)(db => db.Users.Where(u => u.Id == args.id)));

            ScratchResolvers.Context<TestContext>.AddQuery("users", db => db.Users);

            var expr = ScratchResolvers.Resolve<TestContext, User>(db => db.Users);
            using (var db = new TestContext())
            {
                var output = expr.Compile()(db).ToList();

                db.Users
                    .Where(u => u.Id == 0)
                    .Select(u => new
                    {
                        Count = db.Users.Count()
                    });
                new List<int>();

                Console.WriteLine(output);
            }
            GraphQL<TestContext>.Schema.CreateQuery("users", db => db.Users, list: true);
            GraphQL<TestContext>.Schema.CreateQuery("user", new { id = 0 }, (db, args) => db.Users.Where(u => u.Id == args.id));
            //Initialize();

            var queryStr1 = @"
query user(id:1) {
    idAlias : id,
    nameAlias : name,
    account {
        id
    }
}";

            var queryStr2 = @"
query user(id:0) {
    idAlias : id,
    nameAlias : name,
    account {
        id
    }
}";

            var queryStr3 = @"
query users {
    idAlias : id,
    nameAlias : name,
    account {
        id
    }
}";

            var dict = GraphQL<TestContext>.Execute(queryStr1);
            Console.WriteLine(JsonConvert.SerializeObject(dict));

            dict = GraphQL<TestContext>.Execute(queryStr2);
            Console.WriteLine(JsonConvert.SerializeObject(dict));

            dict = GraphQL<TestContext>.Execute(queryStr3);
            Console.WriteLine(JsonConvert.SerializeObject(dict));

            var query = Parser.Parse(queryStr1);
            var executor = new Executor<TestContext>();
            var objs = executor.Execute(query);
            Console.WriteLine(JsonConvert.SerializeObject(objs));

            Console.ReadLine();
        }

        private static void Initialize()
        {
            using (var db = new TestContext())
            {
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
                db.SaveChanges();
            }
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
