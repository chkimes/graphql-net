﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
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

                foreach (var character in StarWarsTestSchema.CreateData())
                {
                    db.Heros.Add(character);
                }

                db.SaveChanges();
            }
        }

        private static GraphQL<EfContext> CreateDefaultContext()
        {
            var schema = GraphQL<EfContext>.CreateDefaultSchema(() => new EfContext());
            schema.AddScalar(new {year = 0, month = 0, day = 0}, ymd => new DateTime(ymd.year, ymd.month, ymd.day));
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
            user.AddPostField("sub", () => new Sub {Id = 1});

            schema.AddType<Sub>().AddField(s => s.Id);
            schema.AddListField("users", db => db.Users);
            schema.AddField("user", new {id = 0}, (db, args) => db.Users.FirstOrDefault(u => u.Id == args.id));
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
            account.AddListField("usersWithActive", new {active = false},
                (db, args, a) => a.Users.Where(u => u.Active == args.active));
            account.AddField("firstUserWithActive", new {active = false},
                (db, args, a) => a.Users.FirstOrDefault(u => u.Active == args.active));

            schema.AddField("account", new {id = 0}, (db, args) => db.Accounts.FirstOrDefault(a => a.Id == args.id));
            schema.AddField
            ("accountPaidBy", new {paid = default(DateTime)},
                (db, args) => db.Accounts.AsQueryable().FirstOrDefault(a => a.PaidUtc <= args.paid));
            schema.AddListField("accountsByGuid", new {guid = Guid.Empty},
                (db, args) => db.Accounts.AsQueryable().Where(a => a.SomeGuid == args.guid));
            schema.AddListField("accountsByType", new {accountType = AccountType.None},
                (db, args) => db.Accounts.AsQueryable().Where(a => a.AccountType == args.accountType));
            schema.AddEnum<AccountType>(prefix: "accountType_");
            //add this enum just so it is part of the schema
            schema.AddEnum<MaterialType>(prefix: "materialType_");
        }

        private static void InitializeMutationSchema(GraphQLSchema<EfContext> schema)
        {
            var mutate = schema.AddType<MutateMe>();
            mutate.AddAllFields();

            schema.AddField("mutateMes", new {id = 0},
                (db, args) => db.MutateMes.AsQueryable().FirstOrDefault(a => a.Id == args.id));
            schema.AddMutation("mutate",
                new {id = 0, newVal = 0},
                (db, args) =>
                {
                    var mutateMe = db.MutateMes.First(m => m.Id == args.id);
                    mutateMe.Value = args.newVal;
                    db.SaveChanges();
                },
                (db, args) => db.MutateMes.AsQueryable().FirstOrDefault(a => a.Id == args.id));
            schema.AddMutation("addMutate",
                new {newVal = 0},
                (db, args) =>
                {
                    var newMutate = new MutateMe {Value = args.newVal};
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
            StarWarsTestSchema.Create(schema, db => db.Heros.AsQueryable());
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
        public void CustomFieldSubQueryUsingContext() =>
            GenericTests.CustomFieldSubQueryUsingContext(CreateDefaultContext());

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
        public void ChildListFieldWithParameters() =>
            GenericTests.ChildListFieldWithParameters(MemContext.CreateDefaultContext());

        [Test]
        public void ChildFieldWithParameters() =>
            GenericTests.ChildFieldWithParameters(MemContext.CreateDefaultContext());

        [Test]
        public static void StarWarsBasicQueryHero() =>
            StarWarsTests.BasicQueryHero(MemContext.CreateDefaultContext());

        [Test]
        public static void StarWarsBasicQueryHeroWithIdAndFriends() =>
            StarWarsTests.BasicQueryHeroWithIdAndFriends(MemContext.CreateDefaultContext());

        [Test]
        public static void StarWarsBasicQueryHeroWithIdAndFriendsOfFriends() =>
            StarWarsTests.BasicQueryHeroWithFriendsOfFriends(MemContext.CreateDefaultContext());

        [Test]
        public static void StarWarsBasicQueryFetchLuke() =>
            StarWarsTests.BasicQueryFetchLuke(MemContext.CreateDefaultContext());
        
        [Test]
        public static void StarWarsFragmentsDuplicatedContent() =>
            StarWarsTests.FragmentsDuplicatedContent(MemContext.CreateDefaultContext());
        
        [Test]
        public static void StarWarsFragmentsAvoidDuplicatedContent() =>
            StarWarsTests.FragmentsAvoidDuplicatedContent(MemContext.CreateDefaultContext());
        
        [Test]
        public static void StarWarsFragmentsInlineFragments() =>
            StarWarsTests.FragmentsInlineFragments(MemContext.CreateDefaultContext());

        [Test]
        public static void StarWarsTypenameR2Droid() =>
            StarWarsTests.TypenameR2Droid(MemContext.CreateDefaultContext());

        [Test]
        public static void StarWarsTypenameLukeHuman() =>
            StarWarsTests.TypenameLukeHuman(MemContext.CreateDefaultContext());

        [Test]
        public static void StarWarsIntrospectionDroidType() =>
            StarWarsTests.IntrospectionDroidType(MemContext.CreateDefaultContext());

        [Test]
        public static void StarWarsIntrospectionDroidTypeKind() =>
            StarWarsTests.IntrospectionDroidTypeKind(MemContext.CreateDefaultContext());

        [Test]
        public static void StarWarsIntrospectionCharacterInterface() =>
            StarWarsTests.IntrospectionCharacterInterface(MemContext.CreateDefaultContext());
        
        [Test]
        public static void UnionTypeStarship() =>
            StarWarsTests.UnionTypeStarship(MemContext.CreateDefaultContext());

        [Test]
        public static void UnionTypeHuman() =>
            StarWarsTests.UnionTypeHuman(MemContext.CreateDefaultContext());

        [Test]
        public static void UnionTypeDroid() =>
            StarWarsTests.UnionTypeDroid(MemContext.CreateDefaultContext());

        [Test]
        public void AddAllFields()
        {
            var schema = GraphQL<EfContext>.CreateDefaultSchema(() => new EfContext());
            schema.AddType<NullRef>().AddAllFields();
            schema.AddType<User>().AddAllFields();
            schema.AddType<Account>().AddAllFields();
            schema.AddField("user", new {id = 0}, (db, args) => db.Users.FirstOrDefault(u => u.Id == args.id));
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

            public EfContext() : base("DefaultConnection")
            {
            }

            protected override void OnModelCreating(DbModelBuilder modelBuilder)
            {
                Database.SetInitializer(new SqliteDropCreateDatabaseWhenModelChanges<EfContext>(modelBuilder));
                modelBuilder.Entity<StarWarsTestSchema.ICharacter>()
                    .HasMany(c => c.Friends)
                    .WithMany();
                base.OnModelCreating(modelBuilder);
            }

            public IDbSet<User> Users { get; set; }
            public IDbSet<Account> Accounts { get; set; }
            public IDbSet<MutateMe> MutateMes { get; set; }
            public IDbSet<NullRef> NullRefs { get; set; }
            public IDbSet<StarWarsTestSchema.ICharacter> Heros { get; set; }
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
            public byte[] ByteArray { get; set; } = {1, 2, 3, 4};

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
    }
}