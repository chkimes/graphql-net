using System;
using FluentNHibernate.Automapping;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Tool.hbm2ddl;

namespace Tests.NH
{
	public class StoreConfiguration : DefaultAutomappingConfiguration
	{
		public override bool ShouldMap(Type type)
		{
			return type.Namespace == "Tests.NH.Entities";
		}
	}

	public class NHibernateRegistry
	{
		public static ISessionFactory GetSessionFactory(string connStr)
		{
			var cfg = new StoreConfiguration();
			return Fluently
				.Configure()
				.Database(MsSqlConfiguration.MsSql2008.ConnectionString(connStr))
				.Mappings(m => m.AutoMappings.Add(AutoMap.AssemblyOf<Entities.User>(cfg)))
				.ExposeConfiguration(MigrationNHibernateConfiguration)
				.BuildSessionFactory();
		}

		private static void MigrationNHibernateConfiguration(NHibernate.Cfg.Configuration configuration)
		{
			var update = new SchemaUpdate(configuration);
			update.Execute(useStdOut: false, doUpdate: true);
		}
	}
}
