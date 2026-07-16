using System.Data.Common;
using Azure.Core;
using Azure.Identity;
using EnterpriseWebLibrary.ExternalFunctionality;
using FluentMigrator.Runner;
using StackExchange.Profiling;
using StackExchange.Profiling.Data;

namespace EnterpriseWebLibrary.DatabaseSpecification.Databases;

/// <summary>
/// Contains information about a MySQL database.
/// </summary>
public class MySqlInfo: DatabaseInfo {
	private static Lazy<ExternalMySqlProvider>? provider;
	private static Lazy<DbProviderFactory>? factory;

	internal static void Init( Func<ExternalMySqlProvider> providerGetter ) {
		provider = new Lazy<ExternalMySqlProvider>( providerGetter );
		factory = new Lazy<DbProviderFactory>( () => provider.Value.GetDbProviderFactory() );
	}

	internal static string GetValidUsername( string user ) => user.TruncateStart( 32 );

	private readonly string secondaryDatabaseName;

	/// <summary>
	/// Gets the server. Returns the empty string to represent the local machine.
	/// </summary>
	public string Server { get; }

	private readonly string database;
	private readonly bool supportsConnectionPooling;

	/// <summary>
	/// Creates a new MySQL information object. Specify the empty string for the secondary database name if this represents the primary database. Pass the empty
	/// string for the server to represent the local machine.
	/// </summary>
	public MySqlInfo( string secondaryDatabaseName, string server, string database, bool supportsConnectionPooling ) {
		this.secondaryDatabaseName = secondaryDatabaseName;
		Server = server;
		this.database = database;
		this.supportsConnectionPooling = supportsConnectionPooling;
	}

	string DatabaseInfo.SecondaryDatabaseName => secondaryDatabaseName;

	string DatabaseInfo.GetDelimitedIdentifier( string databaseObject ) => "`" + databaseObject + "`";
	string DatabaseInfo.ParameterPrefix => "@";
	string DatabaseInfo.LastAutoIncrementValueExpression => "LAST_INSERT_ID()";

	// MySQL no longer has a query cache.
	string DatabaseInfo.QueryCacheHint => "";

	/// <summary>
	/// Gets the username.
	/// </summary>
	public string GetUser() =>
		Environment.GetEnvironmentVariable( $"{EwlStatics.EwlInitialism.EnglishToPascal()}EntraAdminGroupName" ) is {} admin
			? GetValidUsername( admin )
			: Environment.GetEnvironmentVariable( "WEBSITE_SITE_NAME" ) is {} appServiceName /* Azure App Service */
				? GetValidUsername( appServiceName )
				: Environment.GetEnvironmentVariable( "CONTAINER_APP_JOB_NAME" ) is {} containerAppJobName /* Azure Container Apps job */
					? GetValidUsername( containerAppJobName )
					: throw new Exception( "The managed-identity user name is not available." );

	/// <summary>
	/// Gets the password.
	/// </summary>
	public string GetPassword() => getManagedIdentityToken( ManagedIdentityId.SystemAssigned );

	/// <summary>
	/// Gets the database.
	/// </summary>
	public string Database => database;

	/// <summary>
	/// Gets whether the database supports connection pooling.
	/// </summary>
	public bool SupportsConnectionPooling => supportsConnectionPooling;

	string DatabaseInfo.GetConnectionString( int timeout, DatabaseClientIdentity? identityOverride ) =>
		provider!.Value.GetConnectionString(
			Server,
			Server.Length > 0 ? identityOverride is null ? GetUser() : identityOverride.Name : "root",
			Server.Length > 0
				? getManagedIdentityToken(
					identityOverride is null ? ManagedIdentityId.SystemAssigned : ManagedIdentityId.FromUserAssignedClientId( identityOverride.ClientId ) )
				: "password",
			database,
			supportsConnectionPooling,
			(uint)timeout );

	private string getManagedIdentityToken( ManagedIdentityId id ) =>
		new ManagedIdentityCredential( id ).GetToken( new TokenRequestContext( [ "https://ossrdbms-aad.database.windows.net/.default" ] ) ).Token;

	DbConnection DatabaseInfo.CreateConnection( string connectionString ) {
		var connection = factory!.Value.CreateConnection()!;
		connection.ConnectionString = connectionString;
		return connection;
	}

	DbCommand DatabaseInfo.CreateCommand() {
		return new ProfiledDbCommand( factory!.Value.CreateCommand()!, null, MiniProfiler.Current );
	}

	DbParameter DatabaseInfo.CreateParameter() {
		return factory!.Value.CreateParameter()!;
	}

	string DatabaseInfo.GetDbTypeString( object databaseSpecificType ) =>
		Enum.GetName( factory!.Value.GetType().Assembly.GetType( "MySqlConnector.MySqlDbType" )!, databaseSpecificType )!;

	void DatabaseInfo.SetParameterType( DbParameter parameter, string dbTypeString ) {
		var mySqlDbTypeProperty = parameter.GetType().GetProperty( "MySqlDbType" )!;
		mySqlDbTypeProperty.SetValue( parameter, Enum.Parse( factory!.Value.GetType().Assembly.GetType( "MySqlConnector.MySqlDbType" )!, dbTypeString ), null );
	}

	void DatabaseInfo.RegisterDependencyInjectionServicesForMigration( IMigrationRunnerBuilder builder ) {
		provider!.Value.RegisterDependencyInjectionServicesForMigration( builder );
	}
}