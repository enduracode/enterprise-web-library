using System.Data.Common;
using FluentMigrator.Runner;

namespace EnterpriseWebLibrary.ExternalFunctionality;

/// <summary>
/// External MySQL logic.
/// </summary>
public interface ExternalMySqlProvider {
	string GetConnectionString( string server, string userId, string password, string database, bool supportsConnectionPooling, uint timeout );

	DbProviderFactory GetDbProviderFactory();

	void RegisterDependencyInjectionServicesForMigration( IMigrationRunnerBuilder builder );
}