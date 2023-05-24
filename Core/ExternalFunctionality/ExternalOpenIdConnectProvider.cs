using System.Threading.Tasks;
using EnterpriseWebLibrary.EnterpriseWebFramework.OpenIdProvider;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseWebLibrary.ExternalFunctionality;

/// <summary>
/// External OpenID Connect logic.
/// </summary>
public interface ExternalOpenIdConnectProvider {
	/// <summary>
	/// Registers the dependency-injection services needed by the provider.
	/// </summary>
	void RegisterDependencyInjectionServices( IServiceCollection services );

	/// <summary>
	/// Initializes the application-level functionality in the provider.
	/// </summary>
	void InitAppStatics(
		Func<IServiceProvider> currentServicesGetter, string issuerIdentifier, Func<string> certificateGetter, string certificatePassword,
		Func<IEnumerable<OpenIdClient>> clientGetter );

	void InitAppSpecificLogicDependencies();

	void RefreshConfiguration();

	Task<IActionResult> WriteMetadata();

	Task<IActionResult> WriteKeys();

	bool ReadAuthenticationRequest( out string clientIdentifier );

	Task<IActionResult> WriteAuthenticationResponse(
		string clientIdentifier, string subjectIdentifier, IEnumerable<( string name, string value )> additionalClaims );

	Task<IActionResult> WriteAuthenticationErrorResponse();

	Task<IActionResult> WriteTokenResponse();

	Task<IActionResult> WriteUserInfoResponse();
}