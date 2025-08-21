using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.EnterpriseWebFramework.Core.ResourceMetaLogic;
using EnterpriseWebLibrary.SystemSpecificLogic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

// EwlPage
// Parameter: string returnUrl // Not a TrustedUrl because that would cause intermediate-installation links to expire, making automated testing more difficult.
// OptionalParameter: string password
// OptionalParameter: bool hideWarnings
partial class NonLiveLogIn {
	private TrustedResourceInfo returnResource = null!;

	protected override void init() {
		if( !ConfigurationStatics.IsIntermediateInstallation )
			throw new Exception( "installation type" );

		returnResource = ReturnUrl.Length > 0 ? new TrustedExternalResource( new ExternalResource( ReturnUrl ) ) : throw new Exception( "return URL" );
		if( Password.Any() && Password != SystemSpecificLogicStatics.GeneralProvider.IntermediateLogInPassword )
			throw new Exception( "password" );
	}

	protected override string getResourceName() => "Non-Live Installation Log In";
	protected internal override bool IsIntermediateInstallationPublicResource => true;
	protected override UrlHandler getUrlParent() => new Admin.EntitySetup();

	protected override PageContent getContent() {
		if( Password.Any() )
			return new UiPageContent(
				pageLoadPostBack: PostBack.CreateFull( modificationMethod: () => logIn( HideWarnings ), actionGetter: () => new PostBackAction( returnResource ) ) );

		return FormState.ExecuteWithActions(
			PostBack.CreateFull( modificationMethod: () => logIn( false ), actionGetter: () => new PostBackAction( returnResource ) ),
			() => new UiPageContent( contentFootActions: new ButtonSetup( "Log In" ) ).Add(
				FormItemList.CreateStack()
					.AddItem(
						new TextControl(
							"",
							true,
							setup: TextControlSetup.CreateObscured(),
							validationMethod: ( postBackValue, validator ) => {
								// NOTE: Using a single password here is a hack. The real solution is being able to use System Manager credentials, which is a goal.
								var passwordMatch = postBackValue == SystemSpecificLogicStatics.GeneralProvider.IntermediateLogInPassword;
								if( !passwordMatch )
									validator.NoteErrorAndAddMessage( "Incorrect password." );
							} ).ToFormItem( label: "Enter your password for this non-live installation".ToComponents() ) ) ) );
	}

	private void logIn( bool hideWarnings ) {
		NonLiveInstallationStatics.SetIntermediateAuthenticationCookie();
		if( hideWarnings )
			NonLiveInstallationStatics.SetWarningsHiddenCookie();
		RefreshRequestState();
	}
}