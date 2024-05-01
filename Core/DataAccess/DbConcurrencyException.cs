namespace EnterpriseWebLibrary.DataAccess;

/// <summary>
/// An exception caused by a database concurrency error. Do not handle this inside a transaction as the database will have already rolled it back, rendering the
/// connnection unusable until <see cref="DatabaseConnection.RollbackTransaction"/> is called for all nesting levels.
/// </summary>
public class DbConcurrencyException: Exception {
	/// <summary>
	/// Creates a DB concurrency exception.
	/// </summary>
	public DbConcurrencyException( string message, Exception innerException ): base( message, innerException ) {}
}