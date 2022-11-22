using System.Threading.Tasks;
using System.Xml;
using EnterpriseWebLibrary.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

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
		/// Registers the dependency-injection services needed by the provider.
		/// </summary>
		void RegisterDependencyInjectionServices( IServiceCollection services );

		/// <summary>
		/// Initializes the application-level functionality in the provider.
		/// </summary>
		void InitAppStatics(
			Func<IServiceProvider> currentServicesGetter, SystemProviderGetter providerGetter,
			Func<IReadOnlyCollection<( XmlElement metadata, string entityId )>> samlIdentityProviderGetter );

		void InitAppSpecificLogicDependencies();

		void RefreshConfiguration();

		Task<XmlElement> GetMetadata();

		Task WriteLogInResponse( HttpResponse response, string identityProvider, bool forceReauthentication, string returnUrl );

		Task<( string identityProvider, string userName, IReadOnlyDictionary<string, string> attributes, string returnUrl )> ReadAssertion();
	}
}