using System.Security.Cryptography.X509Certificates;
using EnterpriseWebLibrary.EnterpriseWebFramework.OpenIdProvider;

// EwlPage

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Admin;

partial class OpenIdProvider {
	protected override string getResourceName() => "OpenID Provider";

	protected override AlternativeResourceMode createAlternativeMode() =>
		OpenIdProviderStatics.OpenIdProviderEnabled ? null : new DisabledResourceMode( "The OpenID Provider is not enabled in this system." );

	protected override PageContent getContent() {
		var certificate = OpenIdProviderStatics.GetCertificate();
		return new UiPageContent().Add(
			( certificate.Any()
				  ? "Certificate valid until {0}.".FormatWith(
						  new X509Certificate2( Convert.FromBase64String( certificate ), OpenIdProviderStatics.CertificatePassword ).NotAfter
							  .ToDayMonthYearString( false ) )
					  .ToComponents()
				  : "No certificate.".ToComponents() ).Concat( " ".ToComponents() )
			.Append(
				new EwfButton(
					new StandardButtonStyle( "Regenerate", buttonSize: ButtonSize.ShrinkWrap ),
					behavior: new ConfirmationButtonBehavior(
						"Are you sure?".ToComponents(),
						postBack: PostBack.CreateFull(
							"certificate",
							modificationMethod: () =>
								OpenIdProviderStatics.UpdateCertificate(
									UserManagement.GenerateCertificate( DateTimeOffset.UtcNow, OpenIdProviderStatics.CertificatePassword ) ) ) ) ) )
			.Materialize()
			.ToFormItem( label: "System self-signed certificate".ToComponents() )
			.ToComponentCollection() );
	}
}