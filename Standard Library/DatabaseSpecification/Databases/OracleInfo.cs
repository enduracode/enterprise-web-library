using System;
using System.Data.Common;
using System.Reflection;
using MvcMiniProfiler;
using MvcMiniProfiler.Data;

namespace RedStapler.StandardLibrary.DatabaseSpecification.Databases {
	/// <summary>
	/// Contains information about an Oracle database.
	/// </summary>
	public class OracleInfo: DatabaseInfo {
		private static Assembly oracleDataAccess;

		private readonly string secondaryDatabaseName;
		private readonly string dataSource;
		private readonly string userAndSchema;
		private readonly string password;
		private readonly bool supportsConnectionPooling;
		private readonly bool supportsLinguisticIndexes;

		/// <summary>
		/// Creates a new Oracle database information object. Specify the empty string for the secondary database name if this represents the primary database.
		/// </summary>
		public OracleInfo( string secondaryDatabaseName, string dataSource, string userAndSchema, string password, bool supportsConnectionPooling,
		                   bool supportsLinguisticIndexes ) {
			this.secondaryDatabaseName = secondaryDatabaseName;
			this.dataSource = dataSource;
			this.userAndSchema = userAndSchema;
			this.password = password;
			this.supportsConnectionPooling = supportsConnectionPooling;
			this.supportsLinguisticIndexes = supportsLinguisticIndexes;
		}

		string DatabaseInfo.SecondaryDatabaseName { get { return secondaryDatabaseName; } }

		string DatabaseInfo.ParameterPrefix { get { return ":"; } }

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
			// We only want to load up the 64-bit DLL if we are in a 64-bit process.  Being on a 64-bit machine is not sufficient, since the program we are running
			// may be running in 32-bit mode.
			if( Environment.Is64BitProcess ) {
				attemptOracleAssemblyLoad( "2.112.1.0", true );
				attemptOracleAssemblyLoad( "2.111.7.0", true );
				attemptOracleAssemblyLoad( "2.111.6.0", true );
			}
			else {
				attemptOracleAssemblyLoad( "2.112.1.1", false );
				attemptOracleAssemblyLoad( "2.111.6.20", false );
				attemptOracleAssemblyLoad( "2.102.2.20", false );
			}

			if( oracleDataAccess == null )
				throw new ApplicationException( "No suitable Oracle.DataAccess assembly could be found." );

			return
				oracleDataAccess.CreateInstance( "Oracle.DataAccess.Client.OracleConnection",
				                                 false,
				                                 BindingFlags.Default,
				                                 null,
				                                 new object[] { connectionString },
				                                 null,
				                                 null ) as DbConnection;
		}

		private static void attemptOracleAssemblyLoad( string version, bool amd64 ) {
			if( oracleDataAccess == null ) {
				var assembly = "Oracle.DataAccess, Version=" + version + ", Culture=neutral, PublicKeyToken=89b483f429c47342";
				if( amd64 )
					assembly += ", ProcessorArchitecture=" + ProcessorArchitecture.Amd64;
				try {
					oracleDataAccess = Assembly.Load( assembly );
				}
				catch {}
			}
		}

		DbCommand DatabaseInfo.CreateCommand() {
			var c = oracleDataAccess.CreateInstance( "Oracle.DataAccess.Client.OracleCommand" ) as DbCommand;

			// This property would be important if we screwed up the order of parameter adding later on.
			var bindByNameProperty = c.GetType().GetProperty( "BindByName" );
			bindByNameProperty.SetValue( c, true, null );

			return new ProfiledDbCommand( c, null, MiniProfiler.Current );
		}

		DbParameter DatabaseInfo.CreateParameter() {
			return oracleDataAccess.CreateInstance( "Oracle.DataAccess.Client.OracleParameter" ) as DbParameter;
		}

		string DatabaseInfo.GetDbTypeString( object databaseSpecificType ) {
			return Enum.GetName( oracleDataAccess.GetType( "Oracle.DataAccess.Client.OracleDbType" ), databaseSpecificType );
		}

		void DatabaseInfo.SetParameterType( DbParameter parameter, string dbTypeString ) {
			var oracleDbTypeProperty = parameter.GetType().GetProperty( "OracleDbType" );
			oracleDbTypeProperty.SetValue( parameter, Enum.Parse( oracleDataAccess.GetType( "Oracle.DataAccess.Client.OracleDbType" ), dbTypeString ), null );
		}
	}
}