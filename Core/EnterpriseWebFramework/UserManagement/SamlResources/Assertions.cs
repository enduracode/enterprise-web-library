using System.Threading.Tasks;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.ExternalFunctionality;
using EnterpriseWebLibrary.UserManagement;

// EwlResource

namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement.SamlResources;

partial class Assertions {
	protected override void init() {
		if( !AuthenticationStatics.SamlIdentityProviders.Any() )
			throw new ApplicationException( "There are no SAML identity providers enabled in this application." );
	}

	protected internal override bool IsIntermediateInstallationPublicResource => true;

	protected override UrlHandler getUrlParent() => new Metadata();

	protected override bool managesDataAccessCacheInUnsafeRequestMethods => true;

	protected override EwfResponse post() {
		var assertion = Task.Run( async () => await ExternalFunctionalityStatics.ExternalSamlProvider.ReadAssertion() ).Result;

		var identityProvider =
			AuthenticationStatics.SamlIdentityProviders.Single( i => string.Equals( i.EntityId, assertion.identityProvider, StringComparison.Ordinal ) );
		SystemUser user;
		DataAccessState.Current.DisableCache();
		try {
			user = identityProvider.LogInUser( assertion.userName, assertion.attributes );
		}
		finally {
			DataAccessState.Current.ResetCache();
		}

		if( user != null )
			AuthenticationStatics.SetFormsAuthCookieAndUser( user, identityProvider: identityProvider );
		else
			AuthenticationStatics.SetUserLastIdentityProvider( identityProvider );

		try {
			AppRequestState.Instance.CommitDatabaseTransactionsAndExecuteNonTransactionalModificationMethods();
		}
		finally {
			DataAccessState.Current.ResetCache();
		}

		var destinationUrl = new VerifyClientFunctionality( assertion.returnUrl ).GetUrl();
		return EwfResponse.Create(
			ContentTypes.PlainText,
			new EwfResponseBodyCreator( writer => writer.Write( "See Other: {0}".FormatWith( destinationUrl ) ) ),
			statusCodeGetter: () => 303,
			additionalHeaderFieldGetter: () => ( "Location", destinationUrl ).ToCollection() );
	}
}