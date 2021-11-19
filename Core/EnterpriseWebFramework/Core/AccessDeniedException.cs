using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An exception caused by a failed authorization check.
	/// </summary>
	public class AccessDeniedException: ApplicationException {
		internal readonly bool CausedByIntermediateUser;
		internal readonly ResourceBase LogInPage;

		/// <summary>
		/// MVC and internal use only.
		/// </summary>
		public AccessDeniedException( bool causedByIntermediateUser, ResourceBase logInPage ) {
			CausedByIntermediateUser = causedByIntermediateUser;
			LogInPage = logInPage;
		}
	}
}