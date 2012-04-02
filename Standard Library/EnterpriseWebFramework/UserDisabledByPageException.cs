using System;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An exception caused by an attempt to access the authenticated user when it has been disabled by the page.
	/// </summary>
	public class UserDisabledByPageException: ApplicationException {
		/// <summary>
		/// Creates a user disabled by page exception.
		/// </summary>
		public UserDisabledByPageException( string message ): base( message ) {}
	}
}