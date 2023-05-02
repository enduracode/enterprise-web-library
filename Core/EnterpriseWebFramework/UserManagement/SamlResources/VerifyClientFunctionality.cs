// EwlPage
// Parameter: string returnUrl
// OptionalParameter: bool cookiesDisabled
// OptionalParameter: bool clockWrong

namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement.SamlResources;

partial class VerifyClientFunctionality {
	protected override string getResourceName() => "Browser Functionality Verification";

	protected override UrlHandler getUrlParent() => new Metadata();

	protected override PageContent getContent() {
		var clientTime = new DataValue<string>();
		var content = new UiPageContent(
			contentFootActions: verificationFailed()
				                    ? new ButtonSetup(
					                    "Proceed Anyway",
					                    behavior: new PostBackBehavior(
						                    postBack: PostBack.CreateFull( actionGetter: () => new PostBackAction( new ExternalResource( ReturnUrl ) ) ) ) ).ToCollection()
				                    : Enumerable.Empty<ButtonSetup>().Materialize(),
			pageLoadPostBack: verificationFailed()
				                  ? null
				                  : PostBack.CreateFull(
					                  modificationMethod: () => {
						                  if( AuthenticationStatics.TestCookieMissing() )
							                  parametersModification.CookiesDisabled = true;
						                  if( AuthenticationStatics.ClockNotSynchronized( clientTime ) )
							                  parametersModification.ClockWrong = true;
					                  },
					                  actionGetter: () => new PostBackAction( verificationFailed() ? null : new ExternalResource( ReturnUrl ) ) ) );
		if( verificationFailed() ) {
			if( CookiesDisabled )
				content.Add( new Paragraph( Translation.YourBrowserHasCookiesDisabled.ToComponents() ) );
			if( ClockWrong )
				content.Add( new Paragraph( AuthenticationStatics.GetClockWrongMessage().ToComponents() ) );
		}
		else
			content.Add( AuthenticationStatics.GetLogInHiddenFieldsAndSetUpClientSideLogic( clientTime ) );
		return content;
	}

	private bool verificationFailed() => parametersModification.CookiesDisabled || parametersModification.ClockWrong;
}