using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An exception caused by a failed authorization check.
	/// </summary>
	public class AccessDeniedException: ApplicationException {
		private readonly bool causedByIntermediateUser;
		private readonly ResourceInfo logInPage;

		/// <summary>
		/// MVC and internal use only.
		/// </summary>
		public AccessDeniedException( bool causedByIntermediateUser, ResourceInfo logInPage ) {
			this.causedByIntermediateUser = causedByIntermediateUser;
			this.logInPage = logInPage;
		}

		internal bool CausedByIntermediateUser { get { return causedByIntermediateUser; } }
		internal ResourceInfo LogInPage { get { return logInPage; } }
	}
}