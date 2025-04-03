using System.ComponentModel;
using System.Reflection;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.DatabaseSpecification;
using FluentMigrator.Runner;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseWebLibrary;

/// <summary>
/// Generated code use only.
/// </summary>
[ PublicAPI ]
[ EditorBrowsable( EditorBrowsableState.Never ) ]
public static class DataMigrationOps {
	/// <summary>
	/// Generated code use only.
	/// </summary>
	[ EditorBrowsable( EditorBrowsableState.Never ) ]
	public static void MigrateData() {
		var initializationLog = "";
		ConfigurationStatics.Init( "", "Data Migrator", false, ref initializationLog );

		var appAssembly = Assembly.GetCallingAssembly();
		using var serviceProvider = new ServiceCollection().AddFluentMigratorCore()
			.ConfigureRunner(
				builder => builder.addDatabaseServices( ConfigurationStatics.InstallationConfiguration.PrimaryDatabaseInfo )
					.WithGlobalConnectionString( ConfigurationStatics.InstallationConfiguration.PrimaryDatabaseInfo.GetConnectionString( 60 ) )
					.ScanIn( appAssembly )
					.For.Migrations() )
			.AddLogging( builder => builder.AddFluentMigratorConsole() )
			.BuildServiceProvider();
		using var scope = serviceProvider.CreateScope();

		var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
		runner.MigrateUp();
	}

	private static IMigrationRunnerBuilder addDatabaseServices( this IMigrationRunnerBuilder builder, DatabaseInfo databaseInfo ) {
		databaseInfo.RegisterDependencyInjectionServicesForMigration( builder );
		return builder;
	}
}