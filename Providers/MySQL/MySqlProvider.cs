using System.Data.Common;
using EnterpriseWebLibrary.ExternalFunctionality;
using FluentMigrator.Runner;
using MySqlConnector;

namespace EnterpriseWebLibrary.MySql;

public class MySqlProvider: ExternalMySqlProvider {
	string ExternalMySqlProvider.GetConnectionString(
		string server, string userId, string password, string database, bool supportsConnectionPooling, uint timeout ) {
		var builder = new MySqlConnectionStringBuilder();

		builder.Server = server.Length > 0 ? server : "localhost";
		builder.UserID = userId;
		builder.Password = password;
		if( server.Length > 0 )
			builder.SslMode = MySqlSslMode.VerifyFull;
		builder.Database = database;
		if( !supportsConnectionPooling )
			builder.Pooling = false;
		builder.ConnectionTimeout = timeout;

		return builder.ConnectionString;
	}

	DbProviderFactory ExternalMySqlProvider.GetDbProviderFactory() => MySqlConnectorFactory.Instance;

	void ExternalMySqlProvider.RegisterDependencyInjectionServicesForMigration( IMigrationRunnerBuilder builder ) {
		builder.AddMySql8();
	}
}