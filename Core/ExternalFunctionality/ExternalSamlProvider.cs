using System.Xml;
using EnterpriseWebLibrary.Configuration;
using Microsoft.AspNetCore.Http;

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
		void InitAppStatics( SystemProviderGetter providerGetter, Func<IReadOnlyCollection<( XmlElement metadata, string entityId )>> samlIdentityProviderGetter );

		void InitAppSpecificLogicDependencies();

		void RefreshConfiguration();

		XmlElement GetMetadata();

		void WriteLogInResponse( HttpResponse response, string identityProvider, bool forceReauthentication, string returnUrl );

		( string identityProvider, string userName, IReadOnlyDictionary<string, string> attributes, string returnUrl ) ReadAssertion( HttpRequest request );
	}
}