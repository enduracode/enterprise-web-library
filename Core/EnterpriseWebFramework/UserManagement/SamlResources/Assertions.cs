using System.Threading.Tasks;
using EnterpriseWebLibrary.EnterpriseWebFramework.Core;
using EnterpriseWebLibrary.ExternalFunctionality;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement.SamlResources;

// EwlResource
partial class Assertions {
	protected override void init() {
		if( !AuthenticationStatics.SamlIdentityProviders.Any() )
			throw new ApplicationException( "There are no SAML identity providers enabled in this application." );
	}

	protected internal override bool IsIntermediateInstallationPublicResource => true;

	protected override UrlHandler getUrlParent() => new Metadata();

	protected override bool managesDataModificationsInUnsafeRequestMethods => true;

	protected override EwfResponse post() {
		var assertion = Task.Run( ExternalFunctionalityStatics.ExternalSamlProvider.ReadAssertion ).Result;
		if( !assertion.HasValue )
			throw new LogInException();

		bool? authenticationSuccessful = null;
		var identityProvider = AuthenticationStatics.SamlIdentityProviders.Single( i => string.Equals(
			i.EntityId,
			assertion.Value.identityProvider,
			StringComparison.Ordinal ) );
		ExecuteDataModificationMethod( () => {
			var user = identityProvider.LogInUser( assertion.Value.userName, assertion.Value.attributes );
			authenticationSuccessful = user is not null;
			if( authenticationSuccessful.Value )
				AuthenticationStatics.SetFormsAuthCookieAndUser( user!, identityProvider: identityProvider );
			else
				AuthenticationStatics.SetUserLastIdentityProvider( identityProvider );

			AuthenticationStatics.SetTestCookie();
		} );

		var returnUrl = assertion.Value.returnUrl is { Length: > 0 } nonempty
			                ? TrustedUrl.Deserialize( nonempty, EwfConfigurationStatics.AppConfiguration.PublicId )
			                : null;
		if( returnUrl?.TryGetResource( out _ ) != true )
			returnUrl = ( authenticationSuccessful!.Value
				              ? AuthenticationStatics.AppProvider.GetAuthenticatedUserHomeResource()
				              : AuthenticationStatics.GetDefaultLogInPage( null ) ).ToTrustedUrl();

		var destinationUrl = new VerifyClientFunctionality( returnUrl ).GetUrl();
		return EwfResponse.Create(
			ContentTypes.PlainText,
			new EwfResponseBodyCreator( writer => writer.Write( "See Other: {0}".FormatWith( destinationUrl ) ) ),
			statusCodeGetter: () => 303,
			additionalHeaderFieldGetter: () => ( "Location", destinationUrl ).ToCollection() );
	}
}