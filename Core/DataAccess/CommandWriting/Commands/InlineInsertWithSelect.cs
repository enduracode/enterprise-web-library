using EnterpriseWebLibrary.DataAccess.CommandWriting.InlineConditionAbstraction;

namespace EnterpriseWebLibrary.DataAccess.CommandWriting.Commands;

/// <summary>
/// Generated code use only.
/// </summary>
public class InlineInsertWithSelect {
	private readonly string insertTable;
	private readonly IReadOnlyCollection<string> insertColumns;
	private readonly string selectTable;
	private readonly IReadOnlyCollection<string> selectExpressions;
	private readonly List<InlineDbCommandCondition> conditions = new();

	/// <summary>
	/// Create a command that inserts rows in a table using data from another table.
	/// </summary>
	public InlineInsertWithSelect(
		string insertTable, IReadOnlyCollection<string> insertColumns, string selectTable, IReadOnlyCollection<string> selectExpressions ) {
		this.insertTable = insertTable;
		this.insertColumns = insertColumns;
		this.selectTable = selectTable;
		this.selectExpressions = selectExpressions;
	}

	/// <summary>
	/// EWL use only.
	/// </summary>
	public void AddConditions( IEnumerable<InlineDbCommandCondition> conditions ) {
		this.conditions.AddRange( conditions );
	}

	/// <summary>
	/// Executes this command against the specified database connection and returns the number of rows affected.
	/// </summary>
	/// <param name="cn"></param>
	/// <param name="isLongRunning">Pass true to give the command as much time as it needs.</param>
	public int Execute( DBConnection cn, bool isLongRunning = false ) {
		if( conditions.Count == 0 )
			throw new ApplicationException( "Executing an inline-insert-with-select command with no parameters in the where clause is not allowed." );

		var command = cn.DatabaseInfo.CreateCommand();
		command.CommandText = "INSERT INTO {0} ( {1} ) SELECT {2} FROM {3} WHERE ".FormatWith(
			insertTable,
			StringTools.ConcatenateWithDelimiter( ", ", insertColumns ),
			StringTools.ConcatenateWithDelimiter( ", ", selectExpressions ),
			selectTable );

		var first = true;
		var paramNumber = 0;
		foreach( var condition in conditions ) {
			if( !first )
				command.CommandText += " AND ";
			first = false;

			condition.AddToCommand( command, cn.DatabaseInfo, InlineUpdate.GetParamNameFromNumber( paramNumber++ ) );
		}

		return cn.ExecuteNonQueryCommand( command, isLongRunning: isLongRunning );
	}
}