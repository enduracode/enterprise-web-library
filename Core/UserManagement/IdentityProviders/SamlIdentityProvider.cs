using NodaTime;

namespace EnterpriseWebLibrary.UserManagement.IdentityProviders;

public class SamlIdentityProvider: IdentityProvider {
	public delegate SystemUser? LogInUserGetterMethod( string userName, IReadOnlyDictionary<string, string> attributes );

	internal readonly string MetadataUrl;
	internal readonly string EntityId;
	private readonly LogInUserGetterMethod logInUserGetter;
	internal readonly Duration? AuthenticationDuration;

	/// <summary>
	/// Creates a SAML identity provider.
	/// </summary>
	/// <param name="metadataUrl">Do not pass null or the empty string.</param>
	/// <param name="entityId">Do not pass null or the empty string.</param>
	/// <param name="logInUserGetter">A function that takes a SAML subject name identifier and a collection of attributes and returns the corresponding user
	/// object, or null if a user with that identifier does not exist. This function may also update the user if necessary, or even create a new user. Do not
	/// pass null.</param>
	/// <param name="authenticationDuration">The duration of an authentication session. Pass null to use the default. Do not use unless the system absolutely
	/// requires micromanagement of authentication behavior.</param>
	public SamlIdentityProvider( string metadataUrl, string entityId, LogInUserGetterMethod logInUserGetter, Duration? authenticationDuration = null ) {
		MetadataUrl = metadataUrl;
		EntityId = entityId;
		this.logInUserGetter = logInUserGetter;
		AuthenticationDuration = authenticationDuration;
	}

	internal SystemUser? LogInUser( string userName, IReadOnlyDictionary<string, string> attributes ) => logInUserGetter( userName, attributes );
}