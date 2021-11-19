using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using EnterpriseWebLibrary.ExternalFunctionality;
using Tewl.Tools;

// EwlResource

namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement.SamlResources {
	partial class Metadata {
		protected override void init() {
			if( !AuthenticationStatics.SamlIdentityProviders.Any() )
				throw new ApplicationException( "There are no SAML identity providers enabled in this application." );
		}

		protected override UrlHandler getUrlParent() => new Admin.EntitySetup();

		protected override IEnumerable<UrlPattern> getChildUrlPatterns() =>
			LogIn.UrlPatterns.Literal( "log-in" ).ToCollection().Append( Assertions.UrlPatterns.Literal( "assertions" ) );

		protected override EwfSafeRequestHandler getOrHead() =>
			new EwfSafeResponseWriter(
				EwfResponse.Create(
					"application/samlmetadata+xml",
					new EwfResponseBodyCreator(
						( Stream stream ) => {
							using( var writer = XmlWriter.Create( stream, new XmlWriterSettings { Indent = true } ) )
								ExternalFunctionalityStatics.ExternalSamlProvider.GetMetadata().OwnerDocument.Save( writer );
						} ) ) );
	}
}