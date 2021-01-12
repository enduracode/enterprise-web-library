using System.Web;
using EnterpriseWebLibrary.Configuration;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Useful methods that require a web context.
	/// </summary>
	public static class EwfExtensionMethods {
		internal static bool ShouldBeSecureGivenCurrentRequest( this ConnectionSecurity connectionSecurity, bool isIntermediateInstallationPublicPage ) {
			// Intermediate installations must be secure because the intermediate user cookie is secure.
			if( ConfigurationStatics.IsIntermediateInstallation && !isIntermediateInstallationPublicPage )
				return true;

			return connectionSecurity == ConnectionSecurity.MatchingCurrentRequest
				       ? EwfApp.Instance != null && EwfApp.Instance.RequestState != null && EwfApp.Instance.RequestIsSecure( HttpContext.Current.Request )
				       : connectionSecurity == ConnectionSecurity.SecureIfPossible && EwfConfigurationStatics.AppSupportsSecureConnections;
		}

		/// <summary>
		/// Returns true if this request is secure. Always use this method instead of IsSecureConnection.
		/// </summary>
		public static bool IsSecure( this HttpRequest request ) {
			return EwfApp.Instance.RequestIsSecure( request );
		}
	}
}