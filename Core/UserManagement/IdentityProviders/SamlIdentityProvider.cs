namespace EnterpriseWebLibrary.UserManagement.IdentityProviders {
	public class SamlIdentityProvider: IdentityProvider {
		internal readonly string MetadataUrl;

		/// <summary>
		/// Creates a SAML identity provider.
		/// </summary>
		/// <param name="metadataUrl">Do not pass null or the empty string.</param>
		public SamlIdentityProvider( string metadataUrl ) {
			MetadataUrl = metadataUrl;
		}
	}
}