using System;
using System.Data.Common;
using StackExchange.Profiling;
using StackExchange.Profiling.Data;

namespace RedStapler.StandardLibrary.DatabaseSpecification.Databases {
	/// <summary>
	/// Contains information about an Oracle database.
	/// </summary>
	public class OracleInfo: DatabaseInfo {
		private static DbProviderFactory factoryField;
		private static DbProviderFactory factory { get { return factoryField ?? ( factoryField = DbProviderFactories.GetFactory( "Oracle.DataAccess.Client" ) ); } }

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

		string DatabaseInfo.SecondaryDatabaseName { get { return secondaryDatabaseName; } }

		string DatabaseInfo.ParameterPrefix { get { return ":"; } }

		string DatabaseInfo.LastAutoIncrementValueExpression {
			get {
				// Oracle doesn't have identities.
				return "";
			}
		}

		string DatabaseInfo.QueryCacheHint { get { return "/*+ RESULT_CACHE */"; } }

		/// <summary>
		/// Gets the data source.
		/// </summary>
		public string DataSource { get { return dataSource; } }

		/// <summary>
		/// Gets the user/schema.
		/// </summary>
		public string UserAndSchema { get { return userAndSchema; } }

		/// <summary>
		/// Gets the password.
		/// </summary>
		public string Password { get { return password; } }

		/// <summary>
		/// Gets whether the database supports connection pooling.
		/// </summary>
		public bool SupportsConnectionPooling { get { return supportsConnectionPooling; } }

		/// <summary>
		/// Gets whether the database supports linguistic indexes, which impacts whether or not it can enable case-insensitive comparisons.
		/// </summary>
		public bool SupportsLinguisticIndexes { get { return supportsLinguisticIndexes; } }

		DbConnection DatabaseInfo.CreateConnection( string connectionString ) {
			var connection = factory.CreateConnection();
			connection.ConnectionString = connectionString;
			return connection;
		}

		DbCommand DatabaseInfo.CreateCommand() {
			var c = factory.CreateCommand();

			// This property would be important if we screwed up the order of parameter adding later on.
			var bindByNameProperty = c.GetType().GetProperty( "BindByName" );
			bindByNameProperty.SetValue( c, true, null );

			return new ProfiledDbCommand( c, null, MiniProfiler.Current );
		}

		DbParameter DatabaseInfo.CreateParameter() {
			return factory.CreateParameter();
		}

		string DatabaseInfo.GetDbTypeString( object databaseSpecificType ) {
			return Enum.GetName( factory.GetType().Assembly.GetType( "Oracle.DataAccess.Client.OracleDbType" ), databaseSpecificType );
		}

		void DatabaseInfo.SetParameterType( DbParameter parameter, string dbTypeString ) {
			var oracleDbTypeProperty = parameter.GetType().GetProperty( "OracleDbType" );
			oracleDbTypeProperty.SetValue( parameter, Enum.Parse( factory.GetType().Assembly.GetType( "Oracle.DataAccess.Client.OracleDbType" ), dbTypeString ), null );
		}
	}
}