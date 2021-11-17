using System.IO;
using System.Linq;
using System.Xml;
using EnterpriseWebLibrary.ExternalFunctionality;

// EwlResource

namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement {
	partial class SamlMetadata {
		protected override AlternativeResourceMode createAlternativeMode() =>
			AuthenticationStatics.SamlIdentityProviders.Any()
				? null
				: new DisabledResourceMode( "There are no SAML identity providers enabled in this application." );

		protected override UrlHandler getUrlParent() => new Admin.EntitySetup();

		protected override EwfSafeRequestHandler getOrHead() =>
			new EwfSafeResponseWriter(
				EwfResponse.Create(
					"application/samlmetadata+xml",
					new EwfResponseBodyCreator(
						( Stream stream ) => {
							using( var writer = XmlWriter.Create( stream, new XmlWriterSettings { Indent = true } ) )
								ExternalFunctionalityStatics.ExternalSamlProvider.GetMetadataElement().OwnerDocument.Save( writer );
						} ) ) );
	}
}