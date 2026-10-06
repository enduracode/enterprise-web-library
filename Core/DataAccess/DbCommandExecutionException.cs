namespace EnterpriseWebLibrary.DataAccess;

/// <summary>
/// An exception thrown during execution of a database command.
/// </summary>
public class DbCommandExecutionException: Exception {
	/// <summary>
	/// Creates a command-execution exception.
	/// </summary>
	public DbCommandExecutionException( string message, Exception innerException ): base( message, innerException ) {}
}