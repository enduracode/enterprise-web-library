// EwlPage
// Parameter: string returnUrl
// OptionalParameter: bool cookiesDisabled
// OptionalParameter: bool clockWrong

namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement.SamlResources;

partial class VerifyClientFunctionality {
	protected override string getResourceName() => "Browser Functionality Verification";

	protected override UrlHandler getUrlParent() => new Metadata();

	protected override PageContent getContent() {
		if( verificationFailed() )
			return getVerificationFailedContent();

		var clientTime = new DataValue<string>();
		var postBack = PostBack.CreateFull(
			modificationMethod: () => {
				if( AuthenticationStatics.TestCookieMissing() )
					parametersModification.CookiesDisabled = true;
				if( AuthenticationStatics.ClockNotSynchronized( clientTime ) )
					parametersModification.ClockWrong = true;
			},
			actionGetter: () => new PostBackAction( verificationFailed() ? null : new ExternalResource( ReturnUrl ) ) );
		return FormState.ExecuteWithDataModificationsAndDefaultAction(
			postBack.ToCollection(),
			() => new UiPageContent( pageLoadPostBack: postBack ).Add( AuthenticationStatics.GetLogInHiddenFieldsAndSetUpClientSideLogic( clientTime ) ) );
	}

	private PageContent getVerificationFailedContent() {
		var content = new UiPageContent(
			contentFootActions: new ButtonSetup(
					"Proceed Anyway",
					behavior: new PostBackBehavior( postBack: PostBack.CreateFull( actionGetter: () => new PostBackAction( new ExternalResource( ReturnUrl ) ) ) ) )
				.ToCollection() );
		if( CookiesDisabled )
			content.Add( new Paragraph( Translation.YourBrowserHasCookiesDisabled.ToComponents() ) );
		if( ClockWrong )
			content.Add( new Paragraph( AuthenticationStatics.GetClockWrongMessage().ToComponents() ) );
		return content;
	}

	private bool verificationFailed() => parametersModification.CookiesDisabled || parametersModification.ClockWrong;
}