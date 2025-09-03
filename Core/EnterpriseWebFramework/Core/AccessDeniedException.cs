using EnterpriseWebLibrary.EnterpriseWebFramework.Core;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// An exception caused by a failed authorization check.
/// </summary>
public class AccessDeniedException: ApplicationException {
	internal readonly bool CausedByIntermediateUser;
	internal readonly Func<TrustedUrl, ResourceBase?> LogInPageGetter;

	/// <summary>
	/// MVC and internal use only.
	/// </summary>
	public AccessDeniedException( bool causedByIntermediateUser, Func<TrustedUrl, ResourceBase?> logInPageGetter ) {
		CausedByIntermediateUser = causedByIntermediateUser;
		LogInPageGetter = logInPageGetter;
	}
}