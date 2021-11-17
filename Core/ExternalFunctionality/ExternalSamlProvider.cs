using System;
using System.Collections.Generic;
using System.Xml;
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
		void InitStatics( Func<string> certificateGetter, string certificatePassword );

		/// <summary>
		/// Initializes the application-level functionality in the provider.
		/// </summary>
		void InitAppStatics( SystemProviderGetter providerGetter, Func<IReadOnlyCollection<SamlIdentityProvider>> samlIdentityProviderGetter );

		void InitAppSpecificLogicDependencies();

		void RefreshConfiguration();

		XmlElement GetMetadataElement();
	}
}