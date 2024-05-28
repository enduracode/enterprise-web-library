using System.Threading.Tasks;
using EnterpriseWebLibrary.ExternalFunctionality;

// EwlResource

namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement.SamlResources;

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

		var identityProvider =
			AuthenticationStatics.SamlIdentityProviders.Single( i => string.Equals( i.EntityId, assertion.Value.identityProvider, StringComparison.Ordinal ) );
		ExecuteDataModificationMethod(
			() => {
				var user = identityProvider.LogInUser( assertion.Value.userName, assertion.Value.attributes );
				if( user is not null )
					AuthenticationStatics.SetFormsAuthCookieAndUser( user, identityProvider: identityProvider );
				else
					AuthenticationStatics.SetUserLastIdentityProvider( identityProvider );

				AuthenticationStatics.SetTestCookie();
			} );

		var destinationUrl = new VerifyClientFunctionality( assertion.Value.returnUrl ).GetUrl();
		return EwfResponse.Create(
			ContentTypes.PlainText,
			new EwfResponseBodyCreator( writer => writer.Write( "See Other: {0}".FormatWith( destinationUrl ) ) ),
			statusCodeGetter: () => 303,
			additionalHeaderFieldGetter: () => ( "Location", destinationUrl ).ToCollection() );
	}
}