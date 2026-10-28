using System.Data.Common;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;
using FluentMigrator.Runner;

namespace EnterpriseWebLibrary.ExternalFunctionality;

/// <summary>
/// External PostgreSQL logic.
/// </summary>
public interface ExternalPostgresProvider {
	void InitStatics();

	void CleanUpStatics();

	string GetConnectionString( string host, ushort? port, string database, bool supportsConnectionPooling, int timeout );

	DbConnection CreateConnection( string connectionString );

	DbCommand CreateCommand();

	DbParameter CreateParameter();

	void SetParameterType( DbParameter parameter, string dbTypeString );

	string GetDbTypeString( object databaseSpecificType );

	IReadOnlyCollection<InferredParameter> InferStatementParameterTypes( DatabaseConnection connection, string statement );

	Exception CreateConnectionException( Exception originalException, Func<string, string> messageGetter );

	Exception CreateCommandException( Exception originalException, Func<string, string> messageGetter );

	bool RollbackExceptionIsLostTransaction( Exception exception );

	void RegisterDependencyInjectionServicesForMigration( IMigrationRunnerBuilder builder );
}