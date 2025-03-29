using System.Data.Common;
using FluentMigrator.Runner;

namespace EnterpriseWebLibrary.ExternalFunctionality;

/// <summary>
/// External MySQL logic.
/// </summary>
public interface ExternalMySqlProvider {
	DbProviderFactory GetDbProviderFactory();

	void RegisterDependencyInjectionServicesForMigration( IMigrationRunnerBuilder builder );
}