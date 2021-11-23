using System.Collections.Generic;

namespace EnterpriseWebLibrary.UserManagement.IdentityProviders {
	public class SamlIdentityProvider: IdentityProvider {
		public delegate User LogInUserGetterMethod( string userName, IReadOnlyDictionary<string, string> attributes );

		internal readonly string MetadataUrl;
		internal readonly string EntityId;
		private readonly LogInUserGetterMethod logInUserGetter;

		/// <summary>
		/// Creates a SAML identity provider.
		/// </summary>
		/// <param name="metadataUrl">Do not pass null or the empty string.</param>
		/// <param name="entityId">Do not pass null or the empty string.</param>
		/// <param name="logInUserGetter">A function that takes a SAML subject name identifier and a collection of attributes and returns the corresponding user
		/// object, or null if a user with that identifier does not exist. This function may also update the user if necessary. Do not pass null.</param>
		public SamlIdentityProvider( string metadataUrl, string entityId, LogInUserGetterMethod logInUserGetter ) {
			MetadataUrl = metadataUrl;
			EntityId = entityId;
			this.logInUserGetter = logInUserGetter;
		}

		internal User LogInUser( string userName, IReadOnlyDictionary<string, string> attributes ) => logInUserGetter( userName, attributes );
	}
}