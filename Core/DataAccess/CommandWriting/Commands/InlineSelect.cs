using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Humanizer;
using EnterpriseWebLibrary.DataAccess.CommandWriting.InlineConditionAbstraction;

namespace EnterpriseWebLibrary.DataAccess.CommandWriting.Commands {
	/// <summary>
	/// A SELECT query that can be executed against a database.
	/// </summary>
	public class InlineSelect: InlineDbCommandWithConditions {
		private readonly IEnumerable<string> selectExpressions;
		private readonly string fromClause;
		private readonly string orderByClause;
		private readonly bool cacheQueryInDatabase;
		private readonly List<InlineDbCommandCondition> conditions = new List<InlineDbCommandCondition>();

		/// <summary>
		/// Creates a new inline SELECT command.
		/// </summary>
		public InlineSelect( IEnumerable<string> selectExpressions, string fromClause, bool cacheQueryInDatabase, string orderByClause = "" ) {
			this.selectExpressions = selectExpressions;
			this.fromClause = fromClause;
			this.orderByClause = orderByClause;
			this.cacheQueryInDatabase = cacheQueryInDatabase;
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
			command.CommandText =
				"SELECT{0} {1} ".FormatWith(
					cacheQueryInDatabase && cn.DatabaseInfo.QueryCacheHint.Any() ? " {0}".FormatWith( cn.DatabaseInfo.QueryCacheHint ) : "",
					StringTools.ConcatenateWithDelimiter( ", ", selectExpressions.ToArray() ) ) + fromClause;

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