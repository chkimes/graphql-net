using System;
using System.Data.Entity;
using System.Linq;
using EntityFramework.GraphQL;
using Newtonsoft.Json;

namespace ConsoleParser
{
    class ConsoleParser
    {
        static void Main(string[] args)
        {
            //using (var db = new TestContext())
            //{
            //    var account = new Account
            //    {
            //        Name = "My Test Account",
            //        Paid = true
            //    };
            //    db.Accounts.Add(account);
            //    var user = new User
            //    {
            //        Name = "Joe User",
            //        Account = account
            //    };
            //    db.Users.Add(user);
            //    db.SaveChanges();
            //}
                var queryStr = @"
query user {
    idAlias : id,
    nameAlias : name,
    account {
        id
    }
}
";
                var query = new Parser().Parse(queryStr);
                var executor = new Executor<TestContext>();
            var objs = executor.Execute(query);
            Console.WriteLine(JsonConvert.SerializeObject(objs));
                /*
                var expr = GetSelectorExpr(query.Fields, typeof(User));

            using (var db = new TestContext())
            {
                var users = Enumerable.ToList(Queryable.Select(db.Users, (dynamic)expr));
                var serialized = JsonConvert.SerializeObject(users);
                Console.WriteLine(serialized);
            }
            /*
            var parser = new Parser();
            parser.Parse(@"
    query test {
        field1 : aliased (id: x) @directive @directive:value {
            nestedField
            anotherNested
        }
        field2
        field3 {
            moreNests
        }
    }");
    */
            Console.ReadLine();
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
