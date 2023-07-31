using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using StackExchange.Profiling;
using StackExchange.Profiling.Data;

namespace EnterpriseWebLibrary.DatabaseSpecification.Databases;

/// <summary>
/// Contains information about a SQL Server database.
/// </summary>
public class SqlServerInfo: DatabaseInfo {
	private readonly string secondaryDatabaseName;
	private readonly string server;
	private readonly string loginName;
	private readonly string password;
	private readonly string database;
	private readonly bool supportsConnectionPooling;
	private readonly string fullTextCatalog;

	/// <summary>
	/// Creates a new SQL Server information object. Specify the empty string for the secondary database name if this represents the primary database. Pass null
	/// for the server to represent the local machine. Specify null for the login name and password if SQL Server Authentication is not being used. Pass null
	/// for fullTextCatalog to represent no full text catalog.
	/// </summary>
	public SqlServerInfo(
		string secondaryDatabaseName, string server, string loginName, string password, string database, bool supportsConnectionPooling, string fullTextCatalog ) {
		this.secondaryDatabaseName = secondaryDatabaseName;
		this.server = server;
		this.loginName = loginName;
		this.password = password;
		this.database = database;
		this.supportsConnectionPooling = supportsConnectionPooling;
		this.fullTextCatalog = fullTextCatalog;
	}

	string DatabaseInfo.SecondaryDatabaseName => secondaryDatabaseName;

	string DatabaseInfo.ParameterPrefix => "@";
	string DatabaseInfo.LastAutoIncrementValueExpression => "@@IDENTITY";

	// SQL Server doesn't have a query cache.
	string DatabaseInfo.QueryCacheHint => "";

	/// <summary>
	/// Gets the server. Returns null to represent the local machine.
	/// </summary>
	public string Server => server;

	/// <summary>
	/// Gets the SQL Server Authentication login name. Returns null if SQL Server Authentication is not being used.
	/// </summary>
	public string LoginName => loginName;

	/// <summary>
	/// Gets the SQL Server Authentication password. Returns null if SQL Server Authentication is not being used.
	/// </summary>
	public string Password => password;

	/// <summary>
	/// Gets the database.
	/// </summary>
	public string Database => database;

	/// <summary>
	/// Gets whether the database supports connection pooling.
	/// </summary>
	public bool SupportsConnectionPooling => supportsConnectionPooling;

	/// <summary>
	/// Gets the full text catalog name, if it exists.  Otherwise, returns null.
	/// </summary>
	public string FullTextCatalog => fullTextCatalog;

	DbConnection DatabaseInfo.CreateConnection( string connectionString ) => new SqlConnection( connectionString );

	DbCommand DatabaseInfo.CreateCommand() => new ProfiledDbCommand( new SqlCommand(), null, MiniProfiler.Current );

	DbParameter DatabaseInfo.CreateParameter() => new SqlParameter();

	string DatabaseInfo.GetDbTypeString( object databaseSpecificType ) => ( (SqlDbType)databaseSpecificType ).ToString();

	void DatabaseInfo.SetParameterType( DbParameter parameter, string dbTypeString ) {
		( (SqlParameter)parameter ).SqlDbType = dbTypeString.ToEnum<SqlDbType>();
	}
}