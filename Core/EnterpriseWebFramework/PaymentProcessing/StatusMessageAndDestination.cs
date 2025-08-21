using EnterpriseWebLibrary.EnterpriseWebFramework.Core.ResourceMetaLogic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

public class StatusMessageAndDestination {
	internal string Message { get; }
	internal TrustedResourceInfo? Destination { get; }

	/// <summary>
	/// Creates a StatusMessageAndDestination.
	/// </summary>
	/// <param name="message">The status message. Do not pass null.</param>
	/// <param name="destination">The resource to which the user will be redirected. Pass null for no redirection.</param>
	public StatusMessageAndDestination( string message, TrustedResourceInfo? destination ) {
		Message = message;
		Destination = destination;
	}
}