using System;

namespace EnterpriseWebLibrary.DataAccess {
	/// <summary>
	/// An exception caused by a database configuration problem.
	/// </summary>
	public class DbConnectionFailureException: ApplicationException {
		/// <summary>
		/// Creates a DB connection failure exception.
		/// </summary>
		public DbConnectionFailureException( string message, Exception innerException ): base( message, innerException ) {}
	}
}