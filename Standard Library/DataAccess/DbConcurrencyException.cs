using System;

namespace RedStapler.StandardLibrary.DataAccess {
	/// <summary>
	/// An exception caused by a database concurrency error.
	/// </summary>
	public class DbConcurrencyException: ApplicationException {
		/// <summary>
		/// Creates a DB concurrency exception.
		/// </summary>
		public DbConcurrencyException( string message, Exception innerException ): base( message, innerException ) {}
	}
}