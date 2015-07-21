using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using EnterpriseWebLibrary.DatabaseSpecification;

namespace EnterpriseWebLibrary.DataAccess.CommandWriting.Commands {
	/// <summary>
	/// Not yet documented.
	/// </summary>
	public class SprocExecution {
		private readonly string sproc;
		private readonly List<DbCommandParameter> parameters = new List<DbCommandParameter>();

		/// <summary>
		/// Not yet documented.
		/// </summary>
		public SprocExecution( string sproc ) {
			this.sproc = sproc;
		}

		/// <summary>
		/// Adds the specified parameter to this command.
		/// </summary>
		public void AddParameter( DbCommandParameter parameter ) {
			parameters.Add( parameter );
		}

		/// <summary>
		/// Executes this procedure against the specified database connection to get a data reader and then executes the specified method with the reader.
		/// </summary>
		public void ExecuteReader( DBConnection cn, Action<DbDataReader> readerMethod ) {
			var cmd = cn.DatabaseInfo.CreateCommand();
			setupDbCommand( cmd, cn.DatabaseInfo );
			cn.ExecuteReaderCommand( cmd, readerMethod );
		}

		/// <summary>
		/// Executes this sproc against the specified database connection and returns the number of rows affected.
		/// </summary>
		public int ExecuteNonQuery( DBConnection cn ) {
			var cmd = cn.DatabaseInfo.CreateCommand();
			setupDbCommand( cmd, cn.DatabaseInfo );
			return cn.ExecuteNonQueryCommand( cmd );
		}

		/// <summary>
		/// Executes this sproc against the specified database connection and returns a single value.
		/// </summary>
		public object ExecuteScalar( DBConnection cn ) {
			var cmd = cn.DatabaseInfo.CreateCommand();
			setupDbCommand( cmd, cn.DatabaseInfo );
			return cn.ExecuteScalarCommand( cmd );
		}

		private void setupDbCommand( DbCommand dbCmd, DatabaseInfo databaseInfo ) {
			dbCmd.CommandText = sproc;
			dbCmd.CommandType = CommandType.StoredProcedure;
			foreach( var p in parameters )
				dbCmd.Parameters.Add( p.GetAdoDotNetParameter( databaseInfo ) );
		}
	}
}