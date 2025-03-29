using System.Data.Common;
using EnterpriseWebLibrary.ExternalFunctionality;
using FluentMigrator.Runner;
using MySqlConnector;

namespace EnterpriseWebLibrary.MySql;

public class MySqlProvider: ExternalMySqlProvider {
	DbProviderFactory ExternalMySqlProvider.GetDbProviderFactory() => MySqlConnectorFactory.Instance;

	void ExternalMySqlProvider.RegisterDependencyInjectionServicesForMigration( IMigrationRunnerBuilder builder ) {
		builder.AddMySql8();
	}
}