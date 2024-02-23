namespace EnterpriseWebLibrary.DataAccess.CommandWriting.Commands;

/// <summary>
/// Allows simple inserting of rows into a table without the use of any stored procedures.
/// </summary>
public class InlineInsert: InlineDbModificationCommand {
	private readonly string table;
	private readonly List<InlineDbCommandColumnValue> columnModifications = new();

	/// <summary>
	/// Create a command to insert a row in the given table.
	/// </summary>
	public InlineInsert( string table ) {
		this.table = table;
	}

	/// <summary>
	/// Add a data parameter to the command. Value may be null.
	/// </summary>
	public void AddColumnModifications( IEnumerable<InlineDbCommandColumnValue> columnModifications ) {
		this.columnModifications.AddRange( columnModifications );
	}

	/// <summary>
	/// Executes this command against the specified database connection and returns the auto-increment value of the inserted row, or null if it is not an
	/// auto-increment table.
	/// </summary>
	/// <param name="cn"></param>
	/// <param name="isLongRunning">Pass true to give the command as much time as it needs.</param>
	public object? Execute( DatabaseConnection cn, bool isLongRunning = false ) {
		var cmd = cn.DatabaseInfo.CreateCommand();
		cmd.CommandText = "INSERT INTO " + table;
		if( columnModifications.Count == 0 )
			cmd.CommandText += " DEFAULT VALUES";
		else {
			cmd.CommandText += " ( ";
			foreach( var columnMod in columnModifications )
				cmd.CommandText += columnMod.GetColumnIdentifier( cn.DatabaseInfo ) + ", ";
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
		cn.ExecuteNonQueryCommand( cmd, isLongRunning: isLongRunning );

		if( !cn.DatabaseInfo.LastAutoIncrementValueExpression.Any() )
			return null;
		var autoIncrementRetriever = cn.DatabaseInfo.CreateCommand();
		autoIncrementRetriever.CommandText = "SELECT {0}".FormatWith( cn.DatabaseInfo.LastAutoIncrementValueExpression );
		var autoIncrementValue = cn.ExecuteScalarCommand( autoIncrementRetriever );
		return autoIncrementValue != DBNull.Value ? autoIncrementValue : null;
	}
}