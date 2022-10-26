﻿using System.Data.Common;
using StackExchange.Profiling;
using StackExchange.Profiling.Data;

namespace EnterpriseWebLibrary.DatabaseSpecification.Databases {
	/// <summary>
	/// Contains information about a MySQL database.
	/// </summary>
	public class MySqlInfo: DatabaseInfo {
		private static Lazy<DbProviderFactory> factory;

		internal static void Init( Func<DbProviderFactory> dbProviderFactoryGetter ) {
			factory = new Lazy<DbProviderFactory>( dbProviderFactoryGetter );
		}

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

		string DatabaseInfo.SecondaryDatabaseName => secondaryDatabaseName;

		string DatabaseInfo.ParameterPrefix => "@";
		string DatabaseInfo.LastAutoIncrementValueExpression => "LAST_INSERT_ID()";
		string DatabaseInfo.QueryCacheHint => "SQL_CACHE";

		/// <summary>
		/// Gets the database.
		/// </summary>
		public string Database => database;

		/// <summary>
		/// Gets whether the database supports connection pooling.
		/// </summary>
		public bool SupportsConnectionPooling => supportsConnectionPooling;

		DbConnection DatabaseInfo.CreateConnection( string connectionString ) {
			var connection = factory.Value.CreateConnection();
			connection.ConnectionString = connectionString;
			return connection;
		}

		DbCommand DatabaseInfo.CreateCommand() {
			return new ProfiledDbCommand( factory.Value.CreateCommand(), null, MiniProfiler.Current );
		}

		DbParameter DatabaseInfo.CreateParameter() {
			return factory.Value.CreateParameter();
		}

		string DatabaseInfo.GetDbTypeString( object databaseSpecificType ) {
			return Enum.GetName( factory.Value.GetType().Assembly.GetType( "MySql.Data.MySqlClient.MySqlDbType" ), databaseSpecificType );
		}

		void DatabaseInfo.SetParameterType( DbParameter parameter, string dbTypeString ) {
			var mySqlDbTypeProperty = parameter.GetType().GetProperty( "MySqlDbType" );
			mySqlDbTypeProperty.SetValue(
				parameter,
				Enum.Parse( factory.Value.GetType().Assembly.GetType( "MySql.Data.MySqlClient.MySqlDbType" ), dbTypeString ),
				null );
		}
	}
}