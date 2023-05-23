using System.Threading.Tasks;
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
	void InitAppStatics( Func<IServiceProvider> currentServicesGetter, string issuerIdentifier, Func<string> certificateGetter, string certificatePassword );

	void InitAppSpecificLogicDependencies();

	void RefreshConfiguration();

	Task<IActionResult> WriteMetadata();

	Task<IActionResult> WriteKeys();
}