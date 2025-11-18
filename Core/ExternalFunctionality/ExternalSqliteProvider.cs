using System.Data.Common;
using FluentMigrator.Runner;
using Serilog;

namespace EnterpriseWebLibrary.ExternalFunctionality;

/// <summary>
/// External SQLite logic.
/// </summary>
public interface ExternalSqliteProvider {
	string GetConnectionString( string filePath, int timeout );

	DbConnection CreateConnection( string connectionString );

	DbCommand CreateCommand();

	DbParameter CreateParameter();

	void DeleteDatabaseAndReCreateFile( string filePath );

	LoggerConfiguration AddDatabaseAsLogSink( LoggerConfiguration loggerConfiguration, string filePath );

	void RegisterDependencyInjectionServicesForMigration( IMigrationRunnerBuilder builder );
}