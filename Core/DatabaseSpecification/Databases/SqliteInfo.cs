using System.Data.Common;
using EnterpriseWebLibrary.ExternalFunctionality;
using FluentMigrator.Runner;
using StackExchange.Profiling;
using StackExchange.Profiling.Data;

namespace EnterpriseWebLibrary.DatabaseSpecification.Databases;

/// <summary>
/// Contains information about a SQLite database.
/// </summary>
public class SqliteInfo: DatabaseInfo {
	private static Lazy<ExternalSqliteProvider> provider = null!;

	internal static void Init( Func<ExternalSqliteProvider> providerGetter ) {
		provider = new Lazy<ExternalSqliteProvider>( providerGetter );
	}

	private readonly string databaseName;
	private readonly string filePath;
	private readonly bool useReadOnlyMode;

	/// <summary>
	/// Creates a new SQLite information object.
	/// </summary>
	public SqliteInfo( string databaseName, string filePath, bool useReadOnlyMode ) {
		this.databaseName = databaseName;
		this.filePath = filePath;
		this.useReadOnlyMode = useReadOnlyMode;
	}

	string DatabaseInfo.SecondaryDatabaseName => databaseName;

	string DatabaseInfo.GetDelimitedIdentifier( string databaseObject ) =>
		$"""
		 "{databaseObject}"
		 """;

	string DatabaseInfo.ParameterPrefix => ":";
	string DatabaseInfo.LastAutoIncrementValueExpression => throw new NotImplementedException();
	string DatabaseInfo.QueryCacheHint => throw new NotImplementedException();

	string DatabaseInfo.GetConnectionString( int timeout ) => provider.Value.GetConnectionString( filePath, useReadOnlyMode, timeout );

	DbConnection DatabaseInfo.CreateConnection( string connectionString ) => provider.Value.CreateConnection( connectionString );

	DbCommand DatabaseInfo.CreateCommand() => new ProfiledDbCommand( provider.Value.CreateCommand(), null, MiniProfiler.Current );

	DbParameter DatabaseInfo.CreateParameter() => provider.Value.CreateParameter();

	string DatabaseInfo.GetDbTypeString( object databaseSpecificType ) {
		throw new NotImplementedException();
	}

	void DatabaseInfo.SetParameterType( DbParameter parameter, string dbTypeString ) {
		throw new NotImplementedException();
	}

	void DatabaseInfo.RegisterDependencyInjectionServicesForMigration( IMigrationRunnerBuilder builder ) {
		provider.Value.RegisterDependencyInjectionServicesForMigration( builder );
	}
}