using System.Data.Common;
using EnterpriseWebLibrary.DataAccess.CommandWriting.InlineConditionAbstraction;

namespace EnterpriseWebLibrary.DataAccess.CommandWriting.Commands;

/// <summary>
/// A SELECT query that can be executed against a database.
/// </summary>
public class InlineSelect: InlineDbCommandWithConditions {
	private readonly IEnumerable<string> selectExpressions;
	private readonly string fromClause;
	private readonly List<InlineDbCommandCondition> conditions = new();
	private readonly string orderByClause;
	private readonly bool cacheQueryInDatabase;

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
	public void AddConditions( IEnumerable<InlineDbCommandCondition> conditions ) {
		this.conditions.AddRange( conditions );
	}

	/// <summary>
	/// Executes this command using the specified database connection to get a data reader and then executes the specified method with the reader.
	/// </summary>
	/// <param name="cn"></param>
	/// <param name="readerMethod"></param>
	/// <param name="isLongRunning">Pass true to give the command as much time as it needs.</param>
	public void Execute( DatabaseConnection cn, Action<DbDataReader> readerMethod, bool isLongRunning = false ) {
		var command = cn.DatabaseInfo.CreateCommand();
		command.CommandText = "SELECT{0} {1} ".FormatWith(
			                      cacheQueryInDatabase && cn.DatabaseInfo.QueryCacheHint.Any() ? " {0}".FormatWith( cn.DatabaseInfo.QueryCacheHint ) : "",
			                      StringTools.ConcatenateWithDelimiter( ", ", selectExpressions ) ) + fromClause;

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
		cn.ExecuteReaderCommand( command, readerMethod, isLongRunning: isLongRunning );
	}
}