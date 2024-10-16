using EnterpriseWebLibrary.DataAccess.CommandWriting.InlineConditionAbstraction;
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.DataAccess.CommandWriting.Commands;

/// <summary>
/// EWL use only.
/// </summary>
[ PublicAPI ]
public class InlineDelete: InlineDbCommandWithConditions {
	private readonly string tableName;
	private readonly List<InlineDbCommandCondition> conditions = new();

	/// <summary>
	/// Creates a modification that will execute an inline DELETE statement.
	/// </summary>
	public InlineDelete( string tableName ) {
		this.tableName = tableName;
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
	public int Execute( DatabaseConnection cn, bool isLongRunning = false ) {
		var command = cn.DatabaseInfo.CreateCommand();
		command.CommandText = "DELETE FROM " + tableName;

		var first = true;
		var paramNumber = 0;
		foreach( var condition in conditions ) {
			command.CommandText += first ? " WHERE " : " AND ";
			first = false;

			condition.AddToCommand( command, cn.DatabaseInfo, InlineUpdate.GetParamNameFromNumber( paramNumber++ ) );
		}

		return cn.ExecuteNonQueryCommand( command, isLongRunning: isLongRunning );
	}
}