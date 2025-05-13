using System.ComponentModel;
using System.Reflection;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.DatabaseSpecification;
using EnterpriseWebLibrary.DatabaseSpecification.Databases;
using FluentMigrator.Runner;
using FluentMigrator.Runner.VersionTableInfo;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseWebLibrary;

/// <summary>
/// Generated code use only.
/// </summary>
[ PublicAPI ]
[ EditorBrowsable( EditorBrowsableState.Never ) ]
public static class DataMigrationOps {
	private class TableConfiguration: IVersionTableMetaData {
		bool IVersionTableMetaData.OwnsSchema => true;
		string IVersionTableMetaData.SchemaName => "";
		public string TableName => GetMigrationTableName( ConfigurationStatics.InstallationConfiguration.PrimaryDatabaseInfo! );
		string IVersionTableMetaData.ColumnName => isOracle ? "VERSION" : "Version";
		string IVersionTableMetaData.AppliedOnColumnName => isOracle ? "APPLIED_TIME" : "AppliedTime";
		string IVersionTableMetaData.DescriptionColumnName => isOracle ? "DESCRIPTION" : "Description";
		string IVersionTableMetaData.UniqueIndexName => TableName + ( isOracle ? "_VERSION_INDEX" : "VersionIndex" );

		private bool isOracle => ConfigurationStatics.InstallationConfiguration.PrimaryDatabaseInfo is OracleInfo;

		// As of April 2015, returning true causes exceptions because FluentMigrator tries to create both a PK and index with the same name.
		bool IVersionTableMetaData.CreateWithPrimaryKey => false;
	}

	/// <summary>
	/// Generated code use only.
	/// </summary>
	[ EditorBrowsable( EditorBrowsableState.Never ) ]
	public static int MigrateData() {
		var initializationLog = "";
		ConfigurationStatics.Init( "", "Data Migrator", false, ref initializationLog );

		var appAssembly = Assembly.GetCallingAssembly();
		using var serviceProvider = new ServiceCollection().AddFluentMigratorCore()
			.ConfigureRunner(
				builder => builder.addDatabaseServices( ConfigurationStatics.InstallationConfiguration.PrimaryDatabaseInfo! )
					.WithGlobalConnectionString( ConfigurationStatics.InstallationConfiguration.PrimaryDatabaseInfo!.GetConnectionString( 60 ) )
					.ScanIn( appAssembly )
					.For.Migrations() )
			.AddScoped( typeof( IVersionTableMetaData ), typeof( TableConfiguration ) )
			.AddLogging( builder => builder.AddFluentMigratorConsole() )
			.BuildServiceProvider();
		using var scope = serviceProvider.CreateScope();

		var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
		try {
			runner.MigrateUp();
		}
		catch( Exception e ) {
			var outer = DataAccessMethods.CreateDbConnectionException( ConfigurationStatics.InstallationConfiguration.PrimaryDatabaseInfo!, "migrating data in", e );
			Console.Error.WriteLine( outer.Message );
			Console.Error.WriteLine( e.ToString() );
			return 1;
		}

		return 0;
	}

	private static IMigrationRunnerBuilder addDatabaseServices( this IMigrationRunnerBuilder builder, DatabaseInfo databaseInfo ) {
		databaseInfo.RegisterDependencyInjectionServicesForMigration( builder );
		return builder;
	}

	/// <summary>
	/// Development Utility use only.
	/// </summary>
	public static string GetMigrationTableName( DatabaseInfo databaseInfo ) =>
		databaseInfo switch
			{
				MySqlInfo => "{0}_migrations".FormatWith( EwlStatics.EwlInitialism.ToLowerInvariant() ),
				OracleInfo => "{0}_MIGRATIONS".FormatWith( EwlStatics.EwlInitialism.ToUpperInvariant() ),
				_ => "{0}Migrations".FormatWith( EwlStatics.EwlInitialism.EnglishToPascal() )
			};
}