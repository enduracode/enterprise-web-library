#nullable disable
using System.Threading.Tasks;
using EnterpriseWebLibrary.ExternalFunctionality;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.OpenIdProvider;

public class OpenIdAuthenticationResult {
	internal readonly Func<string, Task<IActionResult>> ResponseWriter;
	internal readonly ResourceBase LogInPage;

	/// <summary>
	/// Creates a successful-authentication result.
	/// </summary>
	/// <param name="subjectIdentifier">The subject identifier for the user. Do not pass null or the empty string.</param>
	/// <param name="dataModificationMethod">A method that executes data modifications that happen because of the successful-authentication result, e.g. clearing
	/// cookies that are no longer needed.</param>
	/// <param name="additionalClaims">Additional claims about the user.</param>
	public OpenIdAuthenticationResult(
		string subjectIdentifier, Action dataModificationMethod = null, IEnumerable<( string name, string value )> additionalClaims = null ) {
		ResponseWriter = clientIdentifier => {
			dataModificationMethod?.Invoke();
			return ExternalFunctionalityStatics.ExternalOpenIdConnectProvider.WriteAuthenticationResponse( clientIdentifier, subjectIdentifier, additionalClaims );
		};
	}

	/// <summary>
	/// Creates a failed-authentication result.
	/// </summary>
	/// <param name="logInPage">The log-in page to use. Pass null for default behavior.</param>
	public OpenIdAuthenticationResult( ResourceBase logInPage ) {
		LogInPage = logInPage;
	}
}