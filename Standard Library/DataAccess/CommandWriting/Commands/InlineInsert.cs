using System;
using System.Collections.Generic;
using RedStapler.StandardLibrary.DatabaseSpecification.Databases;

namespace RedStapler.StandardLibrary.DataAccess.CommandWriting.Commands {
	/// <summary>
	/// Allows simple inserting of rows into a table without the use of any stored procedures.
	/// </summary>
	public class InlineInsert: InlineDbModificationCommand {
		private readonly string table;
		private readonly List<InlineDbCommandColumnValue> columnModifications = new List<InlineDbCommandColumnValue>();

		/// <summary>
		/// Create a command to insert a row in the given table.
		/// </summary>
		public InlineInsert( string table ) {
			this.table = table;
		}

		/// <summary>
		/// Add a data parameter to the command. Value may be null.
		/// </summary>
		public void AddColumnModification( InlineDbCommandColumnValue columnModification ) {
			columnModifications.Add( columnModification );
		}

		/// <summary>
		/// Executes this command against the specified database connection and returns the
		/// autonumber ID of the inserted row, or 0 if it is not an autonumber table.
		/// </summary>
		public int Execute( DBConnection cn ) {
			var cmd = cn.DatabaseInfo.CreateCommand();
			cmd.CommandText = "INSERT INTO " + table;
			if( columnModifications.Count == 0 )
				cmd.CommandText += " DEFAULT VALUES";
			else {
				cmd.CommandText += "( ";
				foreach( var columnMod in columnModifications )
					cmd.CommandText += columnMod.ColumnName + ", ";
				cmd.CommandText = cmd.CommandText.Substring( 0, cmd.CommandText.Length - 2 );
				cmd.CommandText += " ) VALUES( ";
				foreach( var columnMod in columnModifications )
					cmd.CommandText += columnMod.Parameter.GetNameForCommandText( cn.DatabaseInfo ) + ", ";
				cmd.CommandText = cmd.CommandText.Substring( 0, cmd.CommandText.Length - 2 );
				cmd.CommandText += " )";

				// setup parameters
				foreach( var columnMod in columnModifications )
					cmd.Parameters.Add( columnMod.Parameter.GetAdoDotNetParameter( cn.DatabaseInfo ) );
			}
			cn.ExecuteNonQueryCommand( cmd );

			object identity = null;
			if( cn.DatabaseInfo is SqlServerInfo ) {
				// Oracle doesn't have identities.
				var identityRetriever = cn.DatabaseInfo.CreateCommand();
				identityRetriever.CommandText = "SELECT @@IDENTITY";
				identity = cn.ExecuteScalarCommand( identityRetriever );
			}

			// NOTE: We should return identity as an object rather than forcing it to be an int. This will eliminate resharper redundant cast warnings in generated
			// mod classes. It will also allow us to return null if there is no value.
			if( identity != null && identity != DBNull.Value )
				return Convert.ToInt32( identity );
			return 0;
		}
	}
}