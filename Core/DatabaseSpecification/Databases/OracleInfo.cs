using System.Data.Common;
using EnterpriseWebLibrary.ExternalFunctionality;
using StackExchange.Profiling;
using StackExchange.Profiling.Data;

namespace EnterpriseWebLibrary.DatabaseSpecification.Databases;

/// <summary>
/// Contains information about an Oracle database.
/// </summary>
public class OracleInfo: DatabaseInfo {
	private static Lazy<ExternalOracleDatabaseProvider>? provider;

	internal static void Init( Func<ExternalOracleDatabaseProvider> providerGetter ) {
		provider = new Lazy<ExternalOracleDatabaseProvider>( providerGetter );
	}

	private readonly string secondaryDatabaseName;
	private readonly string dataSource;
	private readonly string userAndSchema;
	private readonly string password;
	private readonly bool supportsConnectionPooling;
	private readonly bool supportsLinguisticIndexes;

	/// <summary>
	/// Creates a new Oracle database information object. Specify the empty string for the secondary database name if this represents the primary database.
	/// </summary>
	public OracleInfo(
		string secondaryDatabaseName, string dataSource, string userAndSchema, string password, bool supportsConnectionPooling, bool supportsLinguisticIndexes ) {
		this.secondaryDatabaseName = secondaryDatabaseName;
		this.dataSource = dataSource;
		this.userAndSchema = userAndSchema;
		this.password = password;
		this.supportsConnectionPooling = supportsConnectionPooling;
		this.supportsLinguisticIndexes = supportsLinguisticIndexes;
	}

	string DatabaseInfo.SecondaryDatabaseName => secondaryDatabaseName;

	string DatabaseInfo.GetDelimitedIdentifier( string databaseObject ) => "\"" + databaseObject.ToUpperInvariant() + "\"";
	string DatabaseInfo.ParameterPrefix => ":";

	// Oracle doesn't have identities.
	string DatabaseInfo.LastAutoIncrementValueExpression => "";

	string DatabaseInfo.QueryCacheHint => "/*+ RESULT_CACHE */";

	/// <summary>
	/// Gets the data source.
	/// </summary>
	public string DataSource => dataSource;

	/// <summary>
	/// Gets the user/schema.
	/// </summary>
	public string UserAndSchema => userAndSchema;

	/// <summary>
	/// Gets the password.
	/// </summary>
	public string Password => password;

	/// <summary>
	/// Gets whether the database supports connection pooling.
	/// </summary>
	public bool SupportsConnectionPooling => supportsConnectionPooling;

	/// <summary>
	/// Gets whether the database supports linguistic indexes, which impacts whether or not it can enable case-insensitive comparisons.
	/// </summary>
	public bool SupportsLinguisticIndexes => supportsLinguisticIndexes;

	DbConnection DatabaseInfo.CreateConnection( string connectionString ) => provider!.Value.CreateConnection( connectionString );

	DbCommand DatabaseInfo.CreateCommand() => new ProfiledDbCommand( provider!.Value.CreateCommand(), null, MiniProfiler.Current );

	DbParameter DatabaseInfo.CreateParameter() => provider!.Value.CreateParameter();

	string DatabaseInfo.GetDbTypeString( object databaseSpecificType ) => provider!.Value.GetDbTypeString( databaseSpecificType );

	void DatabaseInfo.SetParameterType( DbParameter parameter, string dbTypeString ) {
		provider!.Value.SetParameterType( parameter, dbTypeString );
	}
}