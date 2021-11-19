using System;
using System.Linq;
using System.Web;
using EnterpriseWebLibrary.ExternalFunctionality;
using Humanizer;
using Tewl.Tools;

// EwlResource

namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement {
	partial class SamlAssertions {
		protected override void init() {
			if( !AuthenticationStatics.SamlIdentityProviders.Any() )
				throw new ApplicationException( "There are no SAML identity providers enabled in this application." );
		}

		protected override UrlHandler getUrlParent() => new Admin.EntitySetup();

		protected override EwfResponse post() {
			var assertion = ExternalFunctionalityStatics.ExternalSamlProvider.ReadAssertion( HttpContext.Current.Request );

			var identityProvider =
				AuthenticationStatics.SamlIdentityProviders.Single( i => string.Equals( i.EntityId, assertion.identityProvider, StringComparison.Ordinal ) );
			var user = identityProvider.LogInUser( assertion.userName, assertion.attributes );
			if( user == null )
				throw new ApplicationException( "user" );

			AuthenticationStatics.SetFormsAuthCookieAndUser( user );

			HttpContext.Current.Response.StatusCode = 303;
			return EwfResponse.Create(
				ContentTypes.PlainText,
				new EwfResponseBodyCreator( writer => writer.Write( "See Other: {0}".FormatWith( assertion.returnUrl ) ) ),
				additionalHeaderFieldGetter: () => ( "Location", assertion.returnUrl ).ToCollection() );
		}
	}
}