using FluentMigrator.Runner;
using Serilog;

namespace EnterpriseWebLibrary.ExternalFunctionality;

/// <summary>
/// External SQLite logic.
/// </summary>
public interface ExternalSqliteProvider {
	void DeleteDatabaseAndReCreateFile( string filePath );

	LoggerConfiguration AddDatabaseAsLogSink( LoggerConfiguration loggerConfiguration, string filePath );

	void RegisterDependencyInjectionServicesForMigration( IMigrationRunnerBuilder builder );
}