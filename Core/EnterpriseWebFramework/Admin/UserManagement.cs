using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;
using EnterpriseWebLibrary.UserManagement;
using EnterpriseWebLibrary.UserManagement.IdentityProviders;

// EwlPage

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Admin;

partial class UserManagement {
	internal static string GenerateCertificate( DateTimeOffset currentTime ) {
		using var algorithm = new RSACryptoServiceProvider(
			3072,
			new CspParameters( 24, "Microsoft Enhanced RSA and AES Cryptographic Provider", Guid.NewGuid().ToString() ) );
		var request = new CertificateRequest(
			"CN={0}".FormatWith( ConfigurationStatics.InstallationConfiguration.WebApplications.Single().DefaultBaseUrl.Host ),
			algorithm,
			HashAlgorithmName.SHA256,
			RSASignaturePadding.Pkcs1 );
		request.CertificateExtensions.Add(
			new X509KeyUsageExtension( X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment, false ) );
		using var certificate = request.CreateSelfSigned( currentTime, currentTime.AddYears( 10 ) );
		return Convert.ToBase64String( certificate.Export( X509ContentType.Pfx, UserManagementStatics.CertificatePassword ) );
	}

	protected override AlternativeResourceMode createAlternativeMode() =>
		UserManagementStatics.UserManagementEnabled ? null : new DisabledResourceMode( "User management is not enabled in this system." );

	protected override IEnumerable<UrlPattern> getChildUrlPatterns() => SystemUser.UrlPatterns.UserIdPositiveInt( Es, "create" ).ToCollection();

	protected override PageContent getContent() {
		var content = new UiPageContent( omitContentBox: true );
		if( UserManagementStatics.IdentityProviders.OfType<SamlIdentityProvider>().Any() ) {
			var certificate = UserManagementStatics.GetCertificate();
			content.Add(
				new Section(
					"Identity providers",
					FormItemList.CreateStack(
							items:
							( certificate.Any()
								  ? "Certificate valid until {0}.".FormatWith(
										  new X509Certificate2( Convert.FromBase64String( certificate ), UserManagementStatics.CertificatePassword ).NotAfter.ToDayMonthYearString(
											  false ) )
									  .ToComponents()
								  : "No certificate.".ToComponents() ).Concat( " ".ToComponents() )
							.Append(
								new EwfButton(
									new StandardButtonStyle( "Regenerate", buttonSize: ButtonSize.ShrinkWrap ),
									behavior: new ConfirmationButtonBehavior(
										"Are you sure?".ToComponents(),
										postBack: PostBack.CreateFull(
											"certificate",
											modificationMethod: () => UserManagementStatics.UpdateCertificate( GenerateCertificate( DateTimeOffset.UtcNow ) ) ) ) ) )
							.Materialize()
							.ToFormItem( label: "System self-signed certificate".ToComponents() )
							.Concat(
								AuthenticationStatics.SamlIdentityProviders.Any()
									? new EwfHyperlink( EnterpriseWebFramework.UserManagement.SamlResources.Metadata.GetInfo(), new StandardHyperlinkStyle( "" ) )
										.ToFormItem( label: "Application SAML metadata".ToComponents() )
										.ToCollection()
									: Enumerable.Empty<FormItem>() )
							.Materialize() )
						.ToCollection(),
					style: SectionStyle.Box ) );
		}
		content.Add(
			new Section(
				"System users",
				EwfTable.Create(
						tableActions: new HyperlinkSetup( new SystemUser( Es, null ), "Create User" ).ToCollection(),
						headItems: EwfTableItem.Create( "Email".ToCell().Append( "Role".ToCell() ).Materialize() ).ToCollection() )
					.AddData(
						UserManagementStatics.SystemProvider.GetUsers(),
						user => EwfTableItem.Create(
							user.Email.ToCell().Append( user.Role.Name.ToCell() ).Materialize(),
							setup: EwfTableItemSetup.Create( activationBehavior: ElementActivationBehavior.CreateHyperlink( new SystemUser( Es, user.UserId ) ) ) ) )
					.ToCollection(),
				style: SectionStyle.Box ) );
		return content;
	}
}