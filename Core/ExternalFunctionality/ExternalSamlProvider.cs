using System.Collections.Generic;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.UserManagement.IdentityProviders;

namespace EnterpriseWebLibrary.ExternalFunctionality {
	/// <summary>
	/// External SAML logic.
	/// </summary>
	public interface ExternalSamlProvider {
		/// <summary>
		/// Initializes the provider.
		/// </summary>
		void InitStatics();

		/// <summary>
		/// Initializes the application-level functionality in the provider.
		/// </summary>
		void InitAppStatics( SystemProviderGetter providerGetter );

		void InitAppSpecificLogicDependencies( IReadOnlyCollection<SamlIdentityProvider> samlIdentityProviders );
	}
}