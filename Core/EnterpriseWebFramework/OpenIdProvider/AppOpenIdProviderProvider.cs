#nullable disable
namespace EnterpriseWebLibrary.EnterpriseWebFramework.OpenIdProvider;

/// <summary>
/// Application-specific OpenID Provider logic.
/// </summary>
public abstract class AppOpenIdProviderProvider {
	/// <summary>
	/// Returns a pair of methods for managing the installation’s self-signed certificate, or null if a certificate is not supported.
	/// </summary>
	protected internal virtual ( Func<string> getter, Action<string> updater )? GetCertificateMethods() => null;

	/// <summary>
	/// Returns the clients that are registered with the OpenID Provider.
	/// </summary>
	protected internal abstract IEnumerable<OpenIdClient> GetClients();

	/// <summary>
	/// Authenticates the user for the Authorization Endpoint. 
	/// </summary>
	protected internal abstract OpenIdAuthenticationResult AuthenticateUser( string clientIdentifier );
}