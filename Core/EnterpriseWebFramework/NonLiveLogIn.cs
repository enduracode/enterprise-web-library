using EnterpriseWebLibrary.Configuration;

// EwlPage
// Parameter: string returnUrl
// OptionalParameter: string password
// OptionalParameter: bool hideWarnings

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

partial class NonLiveLogIn {
	protected override void init() {
		if( !ConfigurationStatics.IsIntermediateInstallation )
			throw new ApplicationException( "installation type" );

		if( Password.Any() && Password != ConfigurationStatics.SystemGeneralProvider.IntermediateLogInPassword )
			throw new ApplicationException( "password" );
	}

	protected override string getResourceName() => "Non-Live Installation Log In";
	protected internal override bool IsIntermediateInstallationPublicResource => true;
	protected override UrlHandler getUrlParent() => new Admin.EntitySetup();

	protected override PageContent getContent() {
		if( Password.Any() )
			return new UiPageContent(
				pageLoadPostBack: PostBack.CreateFull(
					modificationMethod: () => logIn( HideWarnings ),
					actionGetter: () => new PostBackAction( new ExternalResource( ReturnUrl ) ) ) ).Add( new Paragraph( "Please wait.".ToComponents() ) );

		return FormState.ExecuteWithDataModificationsAndDefaultAction(
			PostBack.CreateFull( modificationMethod: () => logIn( false ), actionGetter: () => new PostBackAction( new ExternalResource( ReturnUrl ) ) )
				.ToCollection(),
			() => new UiPageContent( contentFootActions: new ButtonSetup( "Log In" ).ToCollection() ).Add(
				FormItemList.CreateStack(
					items: new TextControl(
							"",
							true,
							setup: TextControlSetup.CreateObscured(),
							validationMethod: ( postBackValue, validator ) => {
								// NOTE: Using a single password here is a hack. The real solution is being able to use System Manager credentials, which is a goal.
								var passwordMatch = postBackValue == ConfigurationStatics.SystemGeneralProvider.IntermediateLogInPassword;
								if( !passwordMatch )
									validator.NoteErrorAndAddMessage( "Incorrect password." );
							} ).ToFormItem( label: "Enter your password for this non-live installation".ToComponents() )
						.ToCollection() ) ) );
	}

	private void logIn( bool hideWarnings ) {
		NonLiveInstallationStatics.SetIntermediateAuthenticationCookie();
		AppRequestState.Instance.IntermediateUserExists = true;
		if( hideWarnings )
			NonLiveInstallationStatics.SetWarningsHiddenCookie();
	}
}