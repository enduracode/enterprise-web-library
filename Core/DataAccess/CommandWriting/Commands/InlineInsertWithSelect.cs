using System.Data.Common;
using EnterpriseWebLibrary.DataAccess.CommandWriting.InlineConditionAbstraction;
using EnterpriseWebLibrary.DatabaseSpecification;
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.DataAccess.CommandWriting.Commands;

/// <summary>
/// Generated code use only.
/// </summary>
[ PublicAPI ]
public class InlineInsertWithSelect {
	private readonly string insertTable;
	private readonly IReadOnlyList<string> insertColumns;
	private readonly string selectTable;
	private readonly List<( Func<DatabaseInfo, string> expressionGetter, Action<DatabaseInfo, DbCommand> parameterAdder )> selectExpressions = new();
	private readonly List<InlineDbCommandCondition> conditions = new();

	/// <summary>
	/// Create a command that inserts rows in a table using data from another table.
	/// </summary>
	public InlineInsertWithSelect( string insertTable, IReadOnlyList<string> insertColumns, string selectTable ) {
		this.insertTable = insertTable;
		this.insertColumns = insertColumns;
		this.selectTable = selectTable;
	}

	/// <summary>
	/// EWL use only.
	/// </summary>
	public void AddSelectExpression( string expression ) {
		selectExpressions.Add( ( _ => expression, ( _, _ ) => {} ) );
	}

	/// <summary>
	/// EWL use only.
	/// </summary>
	public void AddSelectValue( DbParameterValue value ) {
		var parameter = new DbCommandParameter( insertColumns[ selectExpressions.Count ], value );
		selectExpressions.Add(
			( parameter.GetNameForCommandText, ( databaseInfo, command ) => command.Parameters.Add( parameter.GetAdoDotNetParameter( databaseInfo ) ) ) );
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
			StringTools.ConcatenateWithDelimiter( ", ", insertColumns.Select( cn.DatabaseInfo.GetDelimitedIdentifier ) ),
			StringTools.ConcatenateWithDelimiter( ", ", selectExpressions.Select( i => i.expressionGetter( cn.DatabaseInfo ) ) ),
			selectTable );
		foreach( var expression in selectExpressions )
			expression.parameterAdder( cn.DatabaseInfo, command );

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