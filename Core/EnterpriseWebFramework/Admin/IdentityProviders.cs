using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using EnterpriseWebLibrary.UserManagement;
using EnterpriseWebLibrary.UserManagement.IdentityProviders;
using Humanizer;
using Tewl.Tools;

// EwlPage

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Admin {
	partial class IdentityProviders {
		private const string certificatePassword = "password";

		protected override AlternativeResourceMode createAlternativeMode() =>
			UserManagementStatics.UserManagementEnabled ? null : new DisabledResourceMode( "User management is not enabled in this system." );

		protected override PageContent getContent() =>
			new UiPageContent( omitContentBox: true ).Add(
				UserManagementStatics.IdentityProviders.OfType<SamlIdentityProvider>()
					.Select(
						identityProvider => {
							var certificate = identityProvider.CertificateGetter();
							return new Section(
								"SAML provider",
								( certificate.Any()
									  ? "Certificate valid until {0}.".FormatWith(
											  new X509Certificate2( Convert.FromBase64String( certificate ), certificatePassword ).NotAfter.ToDayMonthYearString( false ) )
										  .ToComponents()
									  : "No certificate.".ToComponents() ).Concat( " ".ToComponents() )
								.Append(
									new EwfButton(
										new StandardButtonStyle( "Regenerate", buttonSize: ButtonSize.ShrinkWrap ),
										behavior: new PostBackBehavior(
											postBack: PostBack.CreateFull(
												"certificate",
												modificationMethod: () => identityProvider.UpdateCertificate( generateCertificate( DateTimeOffset.UtcNow ) ) ) ) ) )
								.Materialize(),
								style: SectionStyle.Box );
						} )
					.Materialize() );

		private string generateCertificate( DateTimeOffset currentTime ) {
			using( var algorithm = new RSACryptoServiceProvider(
				2048,
				new CspParameters( 24, "Microsoft Enhanced RSA and AES Cryptographic Provider", Guid.NewGuid().ToString() ) ) ) {
				var request = new CertificateRequest( "CN={0}".FormatWith( EwlStatics.EwlName ), algorithm, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1 );
				request.CertificateExtensions.Add(
					new X509KeyUsageExtension( X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment, false ) );
				using( var certificate = request.CreateSelfSigned( currentTime, currentTime.AddYears( 10 ) ) )
					return Convert.ToBase64String( certificate.Export( X509ContentType.Pfx, certificatePassword ) );
			}
		}
	}
}