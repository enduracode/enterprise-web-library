using EnterpriseWebLibrary.DataAccess.CommandWriting.InlineConditionAbstraction;

namespace EnterpriseWebLibrary.DataAccess.CommandWriting.Commands;

/// <summary>
/// This class should only be used by autogenerated code.
/// </summary>
public class InlineUpdate: InlineDbModificationCommand, InlineDbCommandWithConditions {
	/// <summary>
	/// Use this to get a parameter name from a number that should be unique to the query.
	/// </summary>
	internal static string GetParamNameFromNumber( int number ) => "p" + number;

	private readonly string tableName;
	private readonly List<InlineDbCommandColumnValue> columnModifications = new();
	private readonly List<InlineDbCommandCondition> conditions = new();

	/// <summary>
	/// Creates a modification that will execute an inline UPDATE statement.
	/// </summary>
	public InlineUpdate( string tableName ) {
		this.tableName = tableName;
	}

	/// <summary>
	/// Add a data parameter.
	/// </summary>
	public void AddColumnModifications( IEnumerable<InlineDbCommandColumnValue> columnModifications ) {
		this.columnModifications.AddRange( columnModifications );
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
		if( columnModifications.Count == 0 )
			return 0;
		if( conditions.Count == 0 )
			throw new ApplicationException( "Executing an inline update command with no parameters in the where clause is not allowed." );

		var command = cn.DatabaseInfo.CreateCommand();
		command.CommandText = "UPDATE " + tableName + " SET ";
		var paramNumber = 0;
		foreach( var columnMod in columnModifications ) {
			var parameter = columnMod.GetParameter( name: GetParamNameFromNumber( paramNumber++ ) );
			command.CommandText += columnMod.GetColumnIdentifier( cn.DatabaseInfo ) + " = " + parameter.GetNameForCommandText( cn.DatabaseInfo ) + ", ";
			command.Parameters.Add( parameter.GetAdoDotNetParameter( cn.DatabaseInfo ) );
		}
		command.CommandText = command.CommandText.Remove( command.CommandText.Length - 2 );
		command.CommandText += " WHERE ";
		foreach( var condition in conditions ) {
			condition.AddToCommand( command, cn.DatabaseInfo, GetParamNameFromNumber( paramNumber++ ) );
			command.CommandText += " AND ";
		}
		command.CommandText = command.CommandText.Remove( command.CommandText.Length - 5 );
		return cn.ExecuteNonQueryCommand( command, isLongRunning: isLongRunning );
	}
}