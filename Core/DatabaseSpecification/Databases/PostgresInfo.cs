using System.Data.Common;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.ExternalFunctionality;
using EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;
using FluentMigrator.Runner;
using StackExchange.Profiling;
using StackExchange.Profiling.Data;

namespace EnterpriseWebLibrary.DatabaseSpecification.Databases;

/// <summary>
/// Contains information about a PostgreSQL database.
/// </summary>
public class PostgresInfo: DatabaseInfo {
	private static Lazy<ExternalPostgresProvider>? provider;

	internal static void Init( Func<ExternalPostgresProvider> providerGetter ) {
		provider = new Lazy<ExternalPostgresProvider>( providerGetter );
	}

	private readonly string secondaryDatabaseName;
	public string Host { get; }
	public ushort? Port { get; }
	public string Database { get; }
	private readonly bool supportsConnectionPooling;

	/// <summary>
	/// Creates a new PostgreSQL information object. Specify the empty string for the secondary database name if this represents the primary database. Pass the
	/// empty string for the host to represent the local machine.
	/// </summary>
	public PostgresInfo( string secondaryDatabaseName, string host, ushort? port, string database, bool supportsConnectionPooling ) {
		this.secondaryDatabaseName = secondaryDatabaseName;
		Host = host;
		Port = port is 0 ? throw new ArgumentOutOfRangeException( nameof(port) ) : port;
		Database = database;
		this.supportsConnectionPooling = supportsConnectionPooling;
	}

	string DatabaseInfo.SecondaryDatabaseName => secondaryDatabaseName;

	string DatabaseInfo.GetDelimitedIdentifier( string databaseObject ) =>
		$"""
		 "{databaseObject.Replace( "\"", "\"\"", StringComparison.Ordinal )}"
		 """;

	string DatabaseInfo.ParameterPrefix => "@";
	string DatabaseInfo.LastAutoIncrementValueExpression => "";

	// Postgres doesn’t have a query cache.
	string DatabaseInfo.QueryCacheHint => "";

	string DatabaseInfo.GetConnectionString( int timeout, DatabaseClientIdentity? identityOverride ) =>
		provider!.Value.GetConnectionString( Host, Port, Database, supportsConnectionPooling, timeout );

	DbConnection DatabaseInfo.CreateConnection( string connectionString ) => provider!.Value.CreateConnection( connectionString );

	DbCommand DatabaseInfo.CreateCommand() => new ProfiledDbCommand( provider!.Value.CreateCommand(), null, MiniProfiler.Current );

	DbParameter DatabaseInfo.CreateParameter() => provider!.Value.CreateParameter();

	void DatabaseInfo.SetParameterType( DbParameter parameter, string dbTypeString ) => provider!.Value.SetParameterType( parameter, dbTypeString );

	string DatabaseInfo.GetDbTypeString( object databaseSpecificType ) => provider!.Value.GetDbTypeString( databaseSpecificType );

	public IReadOnlyCollection<InferredParameter> InferStatementParameterTypes( DatabaseConnection connection, string statement ) =>
		provider!.Value.InferStatementParameterTypes( connection, statement );

	void DatabaseInfo.RegisterDependencyInjectionServicesForMigration( IMigrationRunnerBuilder builder ) =>
		provider!.Value.RegisterDependencyInjectionServicesForMigration( builder );
}