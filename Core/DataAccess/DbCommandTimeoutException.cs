namespace EnterpriseWebLibrary.DataAccess;

/// <summary>
/// An exception caused by a database command timeout.
/// </summary>
public class DbCommandTimeoutException: Exception {
	internal DbCommandTimeoutException( string message, Exception innerException ): base( message, innerException ) {}
}