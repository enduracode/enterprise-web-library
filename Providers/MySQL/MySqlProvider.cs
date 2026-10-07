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

	Exception ExternalMySqlProvider.CreateCommandException( Exception originalException, Func<string, string> messageGetter ) {
		if( originalException is not MySqlException original )
			return new Exception( messageGetter( "" ), originalException );

		if( original.ErrorCode is MySqlErrorCode.LockDeadlock or MySqlErrorCode.LockWaitTimeout )
			return new DbConcurrencyException( messageGetter( "A concurrency error occurred." ), original );

		if( original.ErrorCode == MySqlErrorCode.CommandTimeoutExpired )
			return new DbCommandTimeoutException( messageGetter( "A command timeout occurred." ), original );

		return new Exception( messageGetter( "Error number: " + original.Number + "." ), original );
	}

	bool ExternalMySqlProvider.RollbackExceptionIsLostTransaction( Exception exception ) =>
		exception is MySqlException { ErrorCode: MySqlErrorCode.StoredProcedureDoesNotExist /* confusing name, but means missing savepoint in this context */ };

	void ExternalMySqlProvider.RegisterDependencyInjectionServicesForMigration( IMigrationRunnerBuilder builder ) {
		builder.AddMySql8();
	}
}