using System;
using System.Data.Common;
using StackExchange.Profiling;
using StackExchange.Profiling.Data;

namespace EnterpriseWebLibrary.DatabaseSpecification.Databases {
	/// <summary>
	/// Contains information about a MySQL database.
	/// </summary>
	public class MySqlInfo: DatabaseInfo {
		private static DbProviderFactory factoryField;
		private static DbProviderFactory factory { get { return factoryField ?? ( factoryField = DbProviderFactories.GetFactory( "MySql.Data.MySqlClient" ) ); } }

		private readonly string secondaryDatabaseName;
		private readonly string database;
		private readonly bool supportsConnectionPooling;

		/// <summary>
		/// Creates a new MySQL information object. Specify the empty string for the secondary database name if this represents the primary database.
		/// </summary>
		public MySqlInfo( string secondaryDatabaseName, string database, bool supportsConnectionPooling ) {
			this.secondaryDatabaseName = secondaryDatabaseName;
			this.database = database;
			this.supportsConnectionPooling = supportsConnectionPooling;
		}

		string DatabaseInfo.SecondaryDatabaseName { get { return secondaryDatabaseName; } }

		string DatabaseInfo.ParameterPrefix { get { return "@"; } }
		string DatabaseInfo.LastAutoIncrementValueExpression { get { return "LAST_INSERT_ID()"; } }
		string DatabaseInfo.QueryCacheHint { get { return "SQL_CACHE"; } }

		/// <summary>
		/// Gets the database.
		/// </summary>
		public string Database { get { return database; } }

		/// <summary>
		/// Gets whether the database supports connection pooling.
		/// </summary>
		public bool SupportsConnectionPooling { get { return supportsConnectionPooling; } }

		DbConnection DatabaseInfo.CreateConnection( string connectionString ) {
			var connection = factory.CreateConnection();
			connection.ConnectionString = connectionString;
			return connection;
		}

		DbCommand DatabaseInfo.CreateCommand() {
			return new ProfiledDbCommand( factory.CreateCommand(), null, MiniProfiler.Current );
		}

		DbParameter DatabaseInfo.CreateParameter() {
			return factory.CreateParameter();
		}

		string DatabaseInfo.GetDbTypeString( object databaseSpecificType ) {
			return Enum.GetName( factory.GetType().Assembly.GetType( "MySql.Data.MySqlClient.MySqlDbType" ), databaseSpecificType );
		}

		void DatabaseInfo.SetParameterType( DbParameter parameter, string dbTypeString ) {
			var mySqlDbTypeProperty = parameter.GetType().GetProperty( "MySqlDbType" );
			mySqlDbTypeProperty.SetValue( parameter, Enum.Parse( factory.GetType().Assembly.GetType( "MySql.Data.MySqlClient.MySqlDbType" ), dbTypeString ), null );
		}
	}
}