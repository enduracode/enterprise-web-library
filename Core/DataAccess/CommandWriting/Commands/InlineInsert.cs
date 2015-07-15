using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;

namespace EnterpriseWebLibrary.DataAccess.CommandWriting.Commands {
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
		/// Executes this command against the specified database connection and returns the auto-increment value of the inserted row, or null if it is not an
		/// auto-increment table.
		/// </summary>
		public object Execute( DBConnection cn ) {
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
				foreach( var columnMod in columnModifications ) {
					var parameter = columnMod.GetParameter();
					cmd.CommandText += parameter.GetNameForCommandText( cn.DatabaseInfo ) + ", ";
					cmd.Parameters.Add( parameter.GetAdoDotNetParameter( cn.DatabaseInfo ) );
				}
				cmd.CommandText = cmd.CommandText.Substring( 0, cmd.CommandText.Length - 2 );
				cmd.CommandText += " )";
			}
			cn.ExecuteNonQueryCommand( cmd );

			if( !cn.DatabaseInfo.LastAutoIncrementValueExpression.Any() )
				return null;
			var autoIncrementRetriever = cn.DatabaseInfo.CreateCommand();
			autoIncrementRetriever.CommandText = "SELECT {0}".FormatWith( cn.DatabaseInfo.LastAutoIncrementValueExpression );
			var autoIncrementValue = cn.ExecuteScalarCommand( autoIncrementRetriever );
			return autoIncrementValue != DBNull.Value ? autoIncrementValue : null;
		}
	}
}