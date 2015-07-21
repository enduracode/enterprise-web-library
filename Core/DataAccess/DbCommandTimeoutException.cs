using System;

namespace EnterpriseWebLibrary.DataAccess {
	/// <summary>
	/// An exception caused by a database command timeout.
	/// </summary>
	public class DbCommandTimeoutException: ApplicationException {
		/// <summary>
		/// Creates a DB command timeout exception.
		/// </summary>
		public DbCommandTimeoutException( string message, Exception innerException ): base( message, innerException ) {}
	}
}