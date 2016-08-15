using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An exception caused by a failed authorization check.
	/// </summary>
	internal class AccessDeniedException: ApplicationException {
		private readonly bool causedByIntermediateUser;
		private readonly PageInfo logInPage;

		internal AccessDeniedException( bool causedByIntermediateUser, PageInfo logInPage ) {
			this.causedByIntermediateUser = causedByIntermediateUser;
			this.logInPage = logInPage;
		}

		internal bool CausedByIntermediateUser { get { return causedByIntermediateUser; } }
		internal PageInfo LogInPage { get { return logInPage; } }
	}
}