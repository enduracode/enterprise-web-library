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
		var assertion = Task.Run( async () => await ExternalFunctionalityStatics.ExternalSamlProvider.ReadAssertion() ).Result;

		var identityProvider =
			AuthenticationStatics.SamlIdentityProviders.Single( i => string.Equals( i.EntityId, assertion.identityProvider, StringComparison.Ordinal ) );
		ExecuteDataModificationMethod(
			() => {
				var user = identityProvider.LogInUser( assertion.userName, assertion.attributes );
				if( user is not null )
					AuthenticationStatics.SetFormsAuthCookieAndUser( user, identityProvider: identityProvider );
				else
					AuthenticationStatics.SetUserLastIdentityProvider( identityProvider );
			} );

		var destinationUrl = new VerifyClientFunctionality( assertion.returnUrl ).GetUrl();
		return EwfResponse.Create(
			ContentTypes.PlainText,
			new EwfResponseBodyCreator( writer => writer.Write( "See Other: {0}".FormatWith( destinationUrl ) ) ),
			statusCodeGetter: () => 303,
			additionalHeaderFieldGetter: () => ( "Location", destinationUrl ).ToCollection() );
	}
}