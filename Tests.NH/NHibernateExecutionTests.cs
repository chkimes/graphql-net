using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data.SqlLocalDb;
using System.Linq;
using GraphQL.Net;
using NHibernate;
using NHibernate.Linq;
using NUnit.Framework;

namespace Tests.NH
{
	[TestFixture]
	public class NHibernateExecutionTests
	{
		private const string SqlLocalDbInstance = "MSSQLLOCALDB";
		private const string DbName = "TestDb";
		private static readonly string ConnStr = $"Data Source=(LocalDB)\\{SqlLocalDbInstance};Initial Catalog={DbName};Integrated Security=True";
		private static ISessionFactory _sessionFactory;

		[OneTimeSetUp]
		public static void Init()
		{
			CreateInstanceAndDb();
			
			_sessionFactory = NHibernateRegistry.GetSessionFactory(ConnStr);

			using (var session = _sessionFactory.OpenSession())
			using (var tran = session.BeginTransaction())
			{
				ClearAllTables(session);
				tran.Commit();
			}

			using (var session = _sessionFactory.OpenSession())
			using (var tran = session.BeginTransaction())
			{	
				InsertData(session, tran);
			}
		}


		private static void CreateInstanceAndDb()
		{
			var provider = new SqlLocalDbProvider();
			var instance = provider.GetOrCreateInstance(SqlLocalDbInstance);
			instance.Start();
			var dbCreateSql =
				$@"IF NOT EXISTS(SELECT 1 from master.sys.databases WHERE name = N'{DbName}')
				BEGIN
					CREATE DATABASE {DbName}
				END";
			using (var conn = instance.CreateConnection())
			{
				var myCommand = new SqlCommand(dbCreateSql, conn);

				conn.Open();
				myCommand.ExecuteNonQuery();
				conn.Close();
			}
		}

		public static void ClearAllTables(ISession session)
		{
			var sql =
			@"
			EXEC sp_MSForEachTable 'DISABLE TRIGGER ALL ON ?'
			EXEC sp_MSForEachTable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'
			EXEC sp_MSForEachTable 'DELETE FROM ?'
			EXEC sp_MSForEachTable 'ALTER TABLE ? CHECK CONSTRAINT ALL'
			EXEC sp_MSForEachTable 'ENABLE TRIGGER ALL ON ?'
			EXEC sp_MSForEachTable	'DBCC CHECKIDENT (''?'', RESEED, 0)'";

			session.CreateSQLQuery(sql).ExecuteUpdate();
		}

		private static void InsertData(ISession session, ITransaction tran)
		{
			var account = new Entities.Account
			{
				Name = "My Test Account",
				Paid = true,
				PaidUtc = new DateTime(2016, 1, 1),
			};
			session.Save(account);
			var user = new Entities.User
			{
				Name = "Joe User",
				Account = account,
				Active = true,
			};
			session.Save(user);
			var account2 = new Entities.Account
			{
				Name = "Another Test Account",
				Paid = false,
			};
			session.Save(account2);
			var user2 = new Entities.User
			{
				Name = "Late Paying User",
				Account = account2
			};
			session.Save(user2);
			session.Save(new Entities.MutateMe());
			tran.Commit();
		}


		[OneTimeTearDown]
		public static void OneTimeTearDown()
		{
			_sessionFactory.Dispose();
			_sessionFactory = null;
		}

		private static GraphQL<TestNhService> CreateDefaultContext(ISession session)
		{
			var schema = GraphQL<TestNhService>.CreateDefaultSchema(() => new TestNhService(session));
			schema.AddScalar(new { year = 0, month = 0, day = 0 }, ymd => new DateTime(ymd.year, ymd.month, ymd.day));
			InitializeUserSchema(schema);
			InitializeAccountSchema(schema);
			InitializeMutationSchema(schema);
			InitializeNullRefSchema(schema);
			schema.Complete();
			return new GraphQL<TestNhService>(schema);
		}

		internal static void InitializeAccountSchema(GraphQLSchema<TestNhService> schema)
		{
			var account = schema.AddType<Entities.Account>();
			account.AddField(a => a.Id);
			account.AddField(a => a.Name);
			account.AddField(a => a.Paid);
			account.AddField(a => a.SomeGuid);
			account.AddField(a => a.ByteArray);
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
		}

		private static void InitializeMutationSchema(GraphQLSchema<TestNhService> schema)
		{
			var mutate = schema.AddType<Entities.MutateMe>();
			mutate.AddAllFields();

			schema.AddField("mutateMes", new { id = 0 }, (db, args) => db.MutateMes.AsQueryable().FirstOrDefault(a => a.Id == args.id));
			schema.AddMutation("mutate",
				new { id = 0, newVal = 0 },
				(db, args) => db.MutateMes.AsQueryable().FirstOrDefault(a => a.Id == args.id),
				(db, args) =>
				{
					var mutateMe = db.MutateMes.First(m => m.Id == args.id);
					mutateMe.Value = args.newVal;
				});
		}

		private static void InitializeUserSchema(GraphQLSchema<TestNhService> schema)
		{
			var user = schema.AddType<Entities.User>();
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

		// mimic an in-memory function
		private static string GetAbcPostField() => "easy as 123";

		private static void InitializeNullRefSchema(GraphQLSchema<TestNhService> schema)
		{
			var nullRef = schema.AddType<Entities.NullRef>();
			nullRef.AddField(n => n.Id);
		}

		[Test]
		public void LookupSingleEntity()
		{
			using (var session = _sessionFactory.OpenSession())
			using (var tran = session.BeginTransaction())
				GenericTests.LookupSingleEntity(CreateDefaultContext(session));
		}

		[Test]
		public void AliasOneField()
		{
			using (var session = _sessionFactory.OpenSession())
			using (var tran = session.BeginTransaction())
				GenericTests.AliasOneField(CreateDefaultContext(session));
		}

		[Test]
		public void NestedEntity()
		{
			using (var session = _sessionFactory.OpenSession())
			using (var tran = session.BeginTransaction())
				GenericTests.NestedEntity(CreateDefaultContext(session));
		}

		[Test]
		public void NoUserQueryReturnsNull()
		{
			using (var session = _sessionFactory.OpenSession())
			using (var tran = session.BeginTransaction())
				GenericTests.NoUserQueryReturnsNull(CreateDefaultContext(session));
		}

		[Test]
		public void CustomFieldSubQuery()
		{
			using (var session = _sessionFactory.OpenSession())
			using (var tran = session.BeginTransaction())
				GenericTests.CustomFieldSubQuery(CreateDefaultContext(session));
		}

		[Test]
		public void CustomFieldSubQueryUsingContext()
		{
			using (var session = _sessionFactory.OpenSession())
			using (var tran = session.BeginTransaction())
				GenericTests.CustomFieldSubQueryUsingContext(CreateDefaultContext(session));
		}

		[Test]
		public void List()
		{
			using (var session = _sessionFactory.OpenSession())
			using (var tran = session.BeginTransaction())
				GenericTests.List(CreateDefaultContext(session));
		}

		[Test]
		public void ListTypeIsList()
		{
			using (var session = _sessionFactory.OpenSession())
			using (var tran = session.BeginTransaction())
				GenericTests.ListTypeIsList(CreateDefaultContext(session));
		}

		[Test]
		public void NestedEntityList()
		{
			using (var session = _sessionFactory.OpenSession())
			using (var tran = session.BeginTransaction())
				GenericTests.NestedEntityList(CreateDefaultContext(session));
		}

		[Test]
		public void PostField()
		{
			using (var session = _sessionFactory.OpenSession())
			using (var tran = session.BeginTransaction())
				GenericTests.PostField(CreateDefaultContext(session));
		}

		[Test]
		public void PostFieldSubQuery()
		{
			using (var session = _sessionFactory.OpenSession())
			using (var tran = session.BeginTransaction())
				GenericTests.PostFieldSubQuery(CreateDefaultContext(session));
		}

		[Test]
		public void TypeName()
		{
			using (var session = _sessionFactory.OpenSession())
			using (var tran = session.BeginTransaction())
				GenericTests.TypeName(CreateDefaultContext(session));
		}

		[Test]
		public void DateTimeFilter()
		{
			using (var session = _sessionFactory.OpenSession())
			using (var tran = session.BeginTransaction())
				GenericTests.DateTimeFilter(CreateDefaultContext(session));
		}

		[Test]
		public void EnumerableSubField()
		{
			using (var session = _sessionFactory.OpenSession())
			using (var tran = session.BeginTransaction())
				GenericTests.EnumerableSubField(CreateDefaultContext(session));
		}

		[Test]
		public void SimpleMutation()
		{
			using (var session = _sessionFactory.OpenSession())
			using (var tran = session.BeginTransaction())
				GenericTests.SimpleMutation(CreateDefaultContext(session));
		}

		[Test]
		public void NullPropagation()
		{
			using (var session = _sessionFactory.OpenSession())
			using (var tran = session.BeginTransaction())
				GenericTests.NullPropagation(CreateDefaultContext(session));
		}

		[Test]
		public void GuidField()
		{
			using (var session = _sessionFactory.OpenSession())
			using (var tran = session.BeginTransaction())
				GenericTests.GuidField(CreateDefaultContext(session));
		}

		[Test]
		public void GuidParameter()
		{
			using (var session = _sessionFactory.OpenSession())
			using (var tran = session.BeginTransaction())
				GenericTests.GuidParameter(CreateDefaultContext(session));
		}

		[Test]
		public void ByteArrayParameter()
		{
			using (var session = _sessionFactory.OpenSession())
			using (var tran = session.BeginTransaction())
				GenericTests.ByteArrayParameter(CreateDefaultContext(session));
		}

		[Test]
		public void ChildListFieldWithParameters()
		{
			using (var session = _sessionFactory.OpenSession())
			using (var tran = session.BeginTransaction())
				GenericTests.ChildListFieldWithParameters(CreateDefaultContext(session));
		}

		[Test]
		public void ChildFieldWithParameters()
		{
			using (var session = _sessionFactory.OpenSession())
			using (var tran = session.BeginTransaction())
				GenericTests.ChildFieldWithParameters(CreateDefaultContext(session));
		}

		[Test]
		public void AddAllFields()
		{
			using (var session = _sessionFactory.OpenSession())
			using (var tran = session.BeginTransaction())
			{
				var testService = new TestNhService(session);
				var schema = GraphQL<TestNhService>.CreateDefaultSchema(() => testService);
				schema.AddType<Entities.User>().AddAllFields();
				schema.AddType<Entities.Account>().AddAllFields();
				schema.AddField("user", new { id = 0 }, (db, args) => db.Users.FirstOrDefault(u => u.Id == args.id));
				schema.Complete();

				var gql = new GraphQL<TestNhService>(schema);
				var results = gql.ExecuteQuery("{ user(id:1) { id, name } }");
				Test.DeepEquals(results, "{ user: { id: 1, name: 'Joe User' } }");
			}
		}

		internal class Sub
		{
			public int Id { get; set; }
		}

		internal class TestNhService
		{
			private readonly ISession _session;
			public TestNhService(ISession session)
			{
				_session = session;
			}

			public IQueryable<Entities.User> Users => _session.Query<Entities.User>();
			public IQueryable<Entities.Account> Accounts => _session.Query<Entities.Account>();
			public IQueryable<Entities.MutateMe> MutateMes => _session.Query<Entities.MutateMe>();
			public IQueryable<Entities.NullRef> NullRefs => _session.Query<Entities.NullRef>();
		}

	}

	namespace Entities
	{
		public class User
		{
			public virtual int Id { get; set; }
			public virtual string Name { get; set; }
			public virtual bool Active { get; set; }

			public virtual Account Account { get; set; }
			public virtual NullRef NullRef { get; set; }
		}

		public class Account
		{
			internal Account()
			{
				Users = new List<User>();
			}

			public virtual int Id { get; set; }
			public virtual string Name { get; set; }
			public virtual bool Paid { get; set; }
			public virtual DateTime? PaidUtc { get; set; }
			public virtual Guid SomeGuid { get; set; }
			public virtual byte[] ByteArray { get; set; } = { 1, 2, 3, 4 };

			public virtual IList<User> Users { get; set; }
		}

		public class MutateMe
		{
			public virtual int Id { get; set; }
			public virtual int Value { get; set; }
		}

		public class NullRef
		{
			public virtual int Id { get; set; }
		}
	}
}
