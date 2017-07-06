using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Linq.Expressions;
using GraphQL.Net;
using NUnit.Framework;
using SQLite.CodeFirst;

namespace Tests.EF
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
                    AccountType = AccountType.Gold
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
                    Paid = false,
                    AccountType = AccountType.Silver
                };
                db.Accounts.Add(account2);
                var user2 = new User
                {
                    Name = "Late Paying User",
                    Account = account2
                };
                db.Users.Add(user2);
                db.MutateMes.Add(new MutateMe());

                var human = new Human
                {
                    Id = 1,
                    Name = "Han Solo",
                    Height = 5.6430448
                };
                db.Heros.Add(human);
                var stormtrooper = new Stormtrooper
                {
                    Id = 2,
                    Name = "FN-2187",
                    Height = 4.9,
                    Specialization = "Imperial Snowtrooper"
                };
                db.Heros.Add(stormtrooper);
                var droid = new Droid
                {
                    Id = 3,
                    Name = "R2-D2",
                    PrimaryFunction = "Astromech"
                };
                db.Heros.Add(droid);
                var vehicle = new Vehicle
                {
                    Id = 1,
                    Name = "Millennium falcon",
                    Human = human
                };
                db.Vehicles.Add(vehicle);

                var vehicle2 = new Vehicle
                {
                    Id = 2,
                    Name = "Speeder bike",
                    Human = stormtrooper
                };
                db.Vehicles.Add(vehicle2);

                db.SaveChanges();
            }
        }

        private static GraphQL<EfContext> CreateDefaultContext()
        {
            var schema = GraphQL<EfContext>.CreateDefaultSchema(() => new EfContext());
            schema.AddScalar(new { year = 0, month = 0, day = 0 }, ymd => new DateTime(ymd.year, ymd.month, ymd.day));
            InitializeUserSchema(schema);
            InitializeAccountSchema(schema);
            InitializeMutationSchema(schema);
            InitializeNullRefSchema(schema);
            InitializeCharacterSchema(schema);
            schema.Complete();
            return new GraphQL<EfContext>(schema);
        }

        private static void InitializeUserSchema(GraphQLSchema<EfContext> schema)
        {
            var user = schema.AddType<User>();
            user.AddField(u => u.Id);
            user.AddField(u => u.Name);
            user.AddField(u => u.Account);
            user.AddField(u => u.NullRef);
            user.AddField("total", (db, u) => db.Users.Count());
            user.AddField("accountPaid", (db, u) => u.Account.Paid);
            user.AddPostField("abc", () => GetAbcPostField());
            user.AddPostField("sub", () => new Sub { Id = 1 });

            schema.AddType<Sub>().AddField(s => s.Id);
            schema.AddListField("users", db => db.Users);
            schema.AddField("user", new { id = 0 }, (db, args) => db.Users.FirstOrDefault(u => u.Id == args.id));
        }

        private static string GetAbcPostField() => "easy as 123"; // mimic an in-memory function

        private static void InitializeAccountSchema(GraphQLSchema<EfContext> schema)
        {
            var account = schema.AddType<Account>();
            account.AddField(a => a.Id);
            account.AddField(a => a.Name);
            account.AddField(a => a.Paid);
            account.AddField(a => a.SomeGuid);
            account.AddField(a => a.ByteArray);
            account.AddField(a => a.AccountType);
            account.AddListField(a => a.Users);
            account.AddListField("activeUsers", (db, a) => a.Users.Where(u => u.Active));
            account.AddListField("usersWithActive", new { active = false }, (db, args, a) => a.Users.Where(u => u.Active == args.active));
            account.AddField("firstUserWithActive", new { active = false }, (db, args, a) => a.Users.FirstOrDefault(u => u.Active == args.active));

            schema.AddField("account", new { id = 0 }, (db, args) => db.Accounts.FirstOrDefault(a => a.Id == args.id));
            schema.AddField
                ("accountPaidBy", new { paid = default(DateTime) },
                    (db, args) => db.Accounts.AsQueryable().FirstOrDefault(a => a.PaidUtc <= args.paid));
            schema.AddListField("accountsByGuid", new { guid = Guid.Empty },
                    (db, args) => db.Accounts.AsQueryable().Where(a => a.SomeGuid == args.guid));
            schema.AddListField("accountsByType", new { accountType = AccountType.None },
                    (db, args) => db.Accounts.AsQueryable().Where(a => a.AccountType == args.accountType));
            schema.AddEnum<AccountType>(prefix: "accountType_");
            //add this enum just so it is part of the schema
            schema.AddEnum<MaterialType>(prefix: "materialType_");
        }

        private static void InitializeMutationSchema(GraphQLSchema<EfContext> schema)
        {
            var mutate = schema.AddType<MutateMe>();
            mutate.AddAllFields();

            schema.AddField("mutateMes", new { id = 0 }, (db, args) => db.MutateMes.AsQueryable().FirstOrDefault(a => a.Id == args.id));
            schema.AddMutation("mutate",
                new { id = 0, newVal = 0 },
                (db, args) =>
                {
                    var mutateMe = db.MutateMes.First(m => m.Id == args.id);
                    mutateMe.Value = args.newVal;
                    db.SaveChanges();
                },
                (db, args) => db.MutateMes.AsQueryable().FirstOrDefault(a => a.Id == args.id));
            schema.AddMutation("addMutate",
                new { newVal = 0 },
                (db, args) =>
                {
                    var newMutate = new MutateMe { Value = args.newVal };
                    db.MutateMes.Add(newMutate);
                    db.SaveChanges();
                    return newMutate.Id;
                },
                (db, args, id) => db.MutateMes.AsQueryable().FirstOrDefault(a => a.Id == id));
        }

        private static void InitializeNullRefSchema(GraphQLSchema<EfContext> schema)
        {
            var nullRef = schema.AddType<NullRef>();
            nullRef.AddField(n => n.Id);
        }

        private static void InitializeCharacterSchema(GraphQLSchema<EfContext> schema)
        {
            var characterInterface = schema.AddInterfaceType<ICharacter>();
            characterInterface.AddAllFields();

            var humanInterface = schema.AddInterfaceType<IHuman>();
            humanInterface.AddAllFields();
            humanInterface.AddInterface(characterInterface);

            var humanType = schema.AddType<Human>();
            humanType.AddAllFields();
            humanType.AddInterface(characterInterface);
            humanType.AddInterface(humanInterface);

            var stormtrooperType = schema.AddType<Stormtrooper>();
            stormtrooperType.AddAllFields();
            stormtrooperType.AddInterface(characterInterface);
            stormtrooperType.AddInterface(humanInterface);

            var droidType = schema.AddType<Droid>();
            droidType.AddAllFields();
            droidType.AddInterface(characterInterface);
            schema.AddUnionType("OtherUnionType01", new List<IGraphQLType>());

            var heroUnionType = schema.AddUnionType(
                "Hero",
                new[]
                {
                    // TODO: ORDER MATTERS FOR TYPENAME RESOLUTION
                    characterInterface.GraphQLType,
                    humanInterface.GraphQLType,
                    humanType.GraphQLType,
                    stormtrooperType.GraphQLType,
                    droidType.GraphQLType
                });


            schema.AddUnionType("OtherUnionType02", new List<IGraphQLType>());

            schema.AddType<Vehicle>().AddAllFields();
            schema.AddField("hero", new {id = 0}, (db, args) => db.Heros.FirstOrDefault(h => h.Id == args.id))
                .WithReturnType(heroUnionType);
            schema.AddListField("heros", db => db.Heros.AsQueryable())
                .WithReturnType(heroUnionType);
        }

        [Test]
        public void LookupSingleEntity() => GenericTests.LookupSingleEntity(CreateDefaultContext());
        [Test]
        public void AliasOneField() => GenericTests.AliasOneField(CreateDefaultContext());
        [Test]
        public void NestedEntity() => GenericTests.NestedEntity(CreateDefaultContext());
        [Test]
        public void NoUserQueryReturnsNull() => GenericTests.NoUserQueryReturnsNull(CreateDefaultContext());
        [Test]
        public void CustomFieldSubQuery() => GenericTests.CustomFieldSubQuery(CreateDefaultContext());
        [Test]
        public void CustomFieldSubQueryUsingContext() => GenericTests.CustomFieldSubQueryUsingContext(CreateDefaultContext());
        [Test]
        public void List() => GenericTests.List(CreateDefaultContext());
        [Test]
        public void ListTypeIsList() => GenericTests.ListTypeIsList(CreateDefaultContext());
        [Test]
        public void NestedEntityList() => GenericTests.NestedEntityList(CreateDefaultContext());
        [Test]
        public void PostField() => GenericTests.PostField(CreateDefaultContext());
        [Test]
        public void PostFieldSubQuery() => GenericTests.PostFieldSubQuery(CreateDefaultContext());
        [Test]
        public void TypeName() => GenericTests.TypeName(CreateDefaultContext());
        [Test]
        public void DateTimeFilter() => GenericTests.DateTimeFilter(CreateDefaultContext());
        [Test]
        public void EnumerableSubField() => GenericTests.EnumerableSubField(CreateDefaultContext());
        [Test]
        public void SimpleMutation() => GenericTests.SimpleMutation(CreateDefaultContext());
        [Test]
        public void MutationWithReturn() => GenericTests.MutationWithReturn(CreateDefaultContext());
        [Test]
        public void NullPropagation() => GenericTests.NullPropagation(CreateDefaultContext());
        [Test]
        public void GuidField() => GenericTests.GuidField(CreateDefaultContext());
        [Test]
        public void GuidParameter() => GenericTests.GuidParameter(CreateDefaultContext());
        [Test]
        public void EnumFieldQuery() => GenericTests.EnumFieldQuery(CreateDefaultContext());
        [Test]
        public void ByteArrayParameter() => GenericTests.ByteArrayParameter(CreateDefaultContext());
        [Test]
        public void ChildListFieldWithParameters() => GenericTests.ChildListFieldWithParameters(MemContext.CreateDefaultContext());
        [Test]
        public void ChildFieldWithParameters() => GenericTests.ChildFieldWithParameters(MemContext.CreateDefaultContext());
        [Test]
        public static void Fragments() => GenericTests.Fragments(CreateDefaultContext());
        [Test]
        public static void InlineFragments() => GenericTests.InlineFragments(CreateDefaultContext());
        [Test]
        public static void InlineFragmentWithListField() => GenericTests.InlineFragmentWithListField(CreateDefaultContext());
        [Test]
        public static void FragmentWithMultiLevelInheritance() => GenericTests.FragmentWithMultiLevelInheritance(CreateDefaultContext());
        [Test]
        public static void InlineFragmentWithoutTypenameField() => GenericTests.InlineFragmentWithoutTypenameField(CreateDefaultContext());
        [Test]
        public static void FragmentWithoutTypenameField() => GenericTests.FragmentWithoutTypenameField(CreateDefaultContext());
        [Test]
        public static void InlineFragmentWithoutTypenameFieldWithoutOtherFields() => GenericTests.InlineFragmentWithoutTypenameFieldWithoutOtherFields(CreateDefaultContext());
        [Test]
        public static void FragmentWithMultipleTypenameFields() => GenericTests.FragmentWithMultipleTypenameFields(CreateDefaultContext());
        [Test]
        public static void FragmentWithMultipleTypenameFieldsMixedWithInlineFragment() => GenericTests.FragmentWithMultipleTypenameFieldsMixedWithInlineFragment(CreateDefaultContext());

        [Test]
        public void AddAllFields()
        {
            var schema = GraphQL<EfContext>.CreateDefaultSchema(() => new EfContext());
            schema.AddType<User>().AddAllFields();
            schema.AddType<Account>().AddAllFields();
            schema.AddField("user", new { id = 0 }, (db, args) => db.Users.FirstOrDefault(u => u.Id == args.id));
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
            static EfContext()
            {
                // This is necessary to make the SQLite provider work with Guids
                Environment.SetEnvironmentVariable("AppendManifestToken_SQLiteProviderManifest", ";BinaryGUID=True;");
            }

            public EfContext() : base("DefaultConnection") { }

            protected override void OnModelCreating(DbModelBuilder modelBuilder)
            {
                Database.SetInitializer(new SqliteDropCreateDatabaseWhenModelChanges<EfContext>(modelBuilder));
                base.OnModelCreating(modelBuilder);
            }

            public IDbSet<User> Users { get; set; }
            public IDbSet<Account> Accounts { get; set; }
            public IDbSet<MutateMe> MutateMes { get; set; }
            public IDbSet<NullRef> NullRefs { get; set; }
            public IDbSet<ICharacter> Heros { get; set; }
            public IDbSet<Vehicle> Vehicles { get; set; }
        }

        class User
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public bool Active { get; set; }

            public int AccountId { get; set; }
            public Account Account { get; set; }

            public int? NullRefId { get; set; }
            public NullRef NullRef { get; set; }
        }

        class Account
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public bool Paid { get; set; }
            public DateTime? PaidUtc { get; set; }
            public Guid SomeGuid { get; set; }
            public byte[] ByteArray { get; set; } = { 1, 2, 3, 4 };

            public AccountType AccountType { get; set; }

            public List<User> Users { get; set; }
        }

        class MutateMe
        {
            public int Id { get; set; }
            public int Value { get; set; }
        }

        class NullRef
        {
            public int Id { get; set; }
        }

        class ICharacter
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
        
        class IHuman : ICharacter
        {
            public double Height { get; set; }
            public ICollection<Vehicle> Vehicles { get; set; }
        }

        class Human : IHuman
        {
            
        }

        class Stormtrooper : IHuman
        {
            public string Specialization { get; set; }
        }

        class Droid : ICharacter
        {
            public string PrimaryFunction { get; set; }
        }

        class Vehicle
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int HumanId { get; set; }
            public virtual IHuman Human { get; set; }
        }
    }
}
