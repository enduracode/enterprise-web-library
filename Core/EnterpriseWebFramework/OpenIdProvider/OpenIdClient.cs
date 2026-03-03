namespace EnterpriseWebLibrary.EnterpriseWebFramework.OpenIdProvider;

public class OpenIdClient {
	public readonly string ClientIdentifier;
	public readonly string ClientName;
	public readonly IReadOnlyCollection<string> RedirectionUrls;

	/// <summary>
	/// Creates an OpenID Provider client.
	/// </summary>
	/// <param name="clientIdentifier">The client identifier. Do not pass null or the empty string.</param>
	/// <param name="redirectionUrls">The redirection URLs used by the client in authentication requests.</param>
	/// <param name="clientName">The name of the client, which may be displayed to the end-user. Do not pass null.</param>
	public OpenIdClient( string clientIdentifier, IReadOnlyCollection<string> redirectionUrls, string clientName = "" ) {
		ClientIdentifier = clientIdentifier;
		ClientName = clientName.Any() ? clientName : clientIdentifier;
		RedirectionUrls = redirectionUrls;
	}
}