using System.Data.Common;
using FluentMigrator.Runner;
using Serilog;

namespace EnterpriseWebLibrary.ExternalFunctionality;

/// <summary>
/// External SQLite logic.
/// </summary>
public interface ExternalSqliteProvider {
	/// <summary>
	/// Initializes the provider.
	/// </summary>
	void InitStatics( string debugLogTimeFormat );

	string GetConnectionString( string filePath, bool useReadOnlyMode, int timeout );

	DbConnection CreateConnection( string connectionString );

	DbCommand CreateCommand();

	DbParameter CreateParameter();

	void DeleteDatabaseAndReCreateFile( string filePath );

	LoggerConfiguration AddDatabaseAsLogSink( LoggerConfiguration loggerConfiguration, string filePath );

	void RegisterDependencyInjectionServicesForMigration( IMigrationRunnerBuilder builder );
}