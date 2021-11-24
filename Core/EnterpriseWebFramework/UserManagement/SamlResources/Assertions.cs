using System;
using System.Linq;
using System.Web;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.ExternalFunctionality;
using EnterpriseWebLibrary.UserManagement;
using Humanizer;
using Tewl.Tools;

// EwlResource

namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement.SamlResources {
	partial class Assertions {
		protected override void init() {
			if( !AuthenticationStatics.SamlIdentityProviders.Any() )
				throw new ApplicationException( "There are no SAML identity providers enabled in this application." );
		}

		protected override UrlHandler getUrlParent() => new Metadata();

		protected override bool managesDataAccessCacheInUnsafeRequestMethods => true;

		protected override EwfResponse post() {
			var assertion = ExternalFunctionalityStatics.ExternalSamlProvider.ReadAssertion( HttpContext.Current.Request );

			var identityProvider =
				AuthenticationStatics.SamlIdentityProviders.Single( i => string.Equals( i.EntityId, assertion.identityProvider, StringComparison.Ordinal ) );
			User user;
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
			HttpContext.Current.Response.StatusCode = 303;
			return EwfResponse.Create(
				ContentTypes.PlainText,
				new EwfResponseBodyCreator( writer => writer.Write( "See Other: {0}".FormatWith( destinationUrl ) ) ),
				additionalHeaderFieldGetter: () => ( "Location", destinationUrl ).ToCollection() );
		}
	}
}