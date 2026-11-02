using System.Data.Common;
using FluentMigrator.Runner;

namespace EnterpriseWebLibrary.ExternalFunctionality;

/// <summary>
/// External Oracle Database logic.
/// </summary>
public interface ExternalOracleDatabaseProvider {
	DbConnection CreateConnection( string connectionString, bool allowVersion11Authentication );

	DbCommand CreateCommand();

	DbParameter CreateParameter();

	void SetParameterType( DbParameter parameter, string dbTypeString );

	string GetDbTypeString( object databaseSpecificType );

	Exception CreateCommandException( Exception originalException, Func<string, string> messageGetter );

	void RegisterDependencyInjectionServicesForMigration( IMigrationRunnerBuilder builder );
}