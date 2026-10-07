using System.Data.Common;
using EnterpriseWebLibrary.DataAccess;
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

	Exception? ExternalMySqlProvider.CreateCommandException( Exception originalException, Func<string, string> messageGetter ) {
		if( new[] { "Deadlock found when trying to get lock", "Lock wait timeout exceeded" }.Any( i =>
			   originalException.Message.Contains( i, StringComparison.Ordinal ) ) )
			return new DbConcurrencyException( messageGetter( "A concurrency error occurred." ), originalException );

		if( originalException.Message.Contains( "Command Timeout expired", StringComparison.Ordinal ) )
			return new DbCommandTimeoutException( messageGetter( "A command timeout occurred." ), originalException );

		return null;
	}

	void ExternalMySqlProvider.RegisterDependencyInjectionServicesForMigration( IMigrationRunnerBuilder builder ) {
		builder.AddMySql8();
	}
}