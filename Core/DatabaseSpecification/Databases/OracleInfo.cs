using System.Data.Common;
using Oracle.ManagedDataAccess.Client;
using StackExchange.Profiling;
using StackExchange.Profiling.Data;
using Tewl.Tools;

namespace EnterpriseWebLibrary.DatabaseSpecification.Databases {
	/// <summary>
	/// Contains information about an Oracle database.
	/// </summary>
	public class OracleInfo: DatabaseInfo {
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

		DbConnection DatabaseInfo.CreateConnection( string connectionString ) {
			return new OracleConnection( connectionString );
		}

		DbCommand DatabaseInfo.CreateCommand() {
			var c = new OracleCommand();

			// This property would be important if we screwed up the order of parameter adding later on.
			c.BindByName = true;

			// Tell the data reader to retrieve LOB data along with the rest of the row rather than making a separate request when GetValue is called.
			// Unfortunately, as of 17 July 2014 there is an Oracle bug that prevents us from setting the property to -1. See
			// http://stackoverflow.com/q/9006773/35349, https://community.oracle.com/thread/3548124, and Oracle bugs 14279177 and 17869834.
			//c.InitialLOBFetchSize = -1;
			c.InitialLOBFetchSize = 1024;

			return new ProfiledDbCommand( c, null, MiniProfiler.Current );
		}

		DbParameter DatabaseInfo.CreateParameter() {
			return new OracleParameter();
		}

		string DatabaseInfo.GetDbTypeString( object databaseSpecificType ) {
			return ( (OracleDbType)databaseSpecificType ).ToString();
		}

		void DatabaseInfo.SetParameterType( DbParameter parameter, string dbTypeString ) {
			( (OracleParameter)parameter ).OracleDbType = dbTypeString.ToEnum<OracleDbType>();
		}
	}
}