using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using RedStapler.StandardLibrary.DataAccess.CommandWriting.InlineConditionAbstraction;

namespace RedStapler.StandardLibrary.DataAccess.CommandWriting.Commands {
	/// <summary>
	/// A SELECT query that can be executed against a database.
	/// </summary>
	public class InlineSelect: InlineDbCommandWithConditions {
		private readonly string selectFromClause;
		private readonly string orderByClause;
		private readonly List<InlineDbCommandCondition> conditions = new List<InlineDbCommandCondition>();

		/// <summary>
		/// Creates a new inline SELECT command.
		/// </summary>
		public InlineSelect( string selectFromClause, string orderByClause = "" ) {
			this.selectFromClause = selectFromClause;
			this.orderByClause = orderByClause;
		}

		/// <summary>
		/// Adds a condition to the command.
		/// </summary>
		public void AddCondition( InlineDbCommandCondition condition ) {
			conditions.Add( condition );
		}

		/// <summary>
		/// Executes this command using the specified database connection to get a data reader and then executes the specified method with the reader.
		/// </summary>
		public void Execute( DBConnection cn, Action<DbDataReader> readerMethod ) {
			var command = cn.DatabaseInfo.CreateCommand();
			command.CommandText = selectFromClause;

			if( conditions.Any() ) {
				command.CommandText += " WHERE ";
				var first = true;
				var paramNumber = 0;
				foreach( var condition in conditions ) {
					if( !first )
						command.CommandText += " AND ";
					first = false;
					condition.AddToCommand( command, cn.DatabaseInfo, InlineUpdate.GetParamNameFromNumber( paramNumber++ ) );
				}
			}

			command.CommandText = command.CommandText.ConcatenateWithSpace( orderByClause );
			cn.ExecuteReaderCommand( command, readerMethod );
		}
	}
}