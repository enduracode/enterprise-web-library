using EnterpriseWebLibrary.EnterpriseWebFramework.Core.ResourceMetaLogic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement.SamlResources;

// EwlPage
// Parameter: returnUrl
// OptionalParameter: bool cookiesDisabled
// OptionalParameter: Duration? clockError
partial class VerifyClientFunctionality {
	private TrustedResourceInfo returnResource = null!;

	protected override void init() {
		returnResource = ReturnUrl.GetResourceOrThrow();
	}

	protected override string getResourceName() => "Browser Functionality Verification";

	protected override UrlHandler getUrlParent() => new Metadata();

	protected override PageContent getContent() {
		if( verificationFailed() )
			return getVerificationFailedContent();

		var clientTime = new DataValue<string>( false );
		var postBack = PostBack.CreateFull(
			modificationMethod: () => {
				if( AuthenticationStatics.TestCookieMissing() )
					parametersModification.CookiesDisabled = true;
				parametersModification.ClockError = AuthenticationStatics.GetClockError( clientTime );
			},
			actionGetter: () => new PostBackAction(
				verificationFailed() ? null : returnResource,
				authorizationCheckDisabledPredicate: verificationFailed() ? null : _ => true ) );
		return FormState.ExecuteWithActions(
			postBack,
			() => new UiPageContent( pageLoadPostBack: postBack ).Add( AuthenticationStatics.GetLogInHiddenFields( clientTime ) ) );
	}

	private PageContent getVerificationFailedContent() {
		var content = new UiPageContent(
			contentFootActions: new ButtonSetup(
				"Proceed Anyway",
				behavior: new PostBackBehavior(
					postBack: PostBack.CreateFull( actionGetter: () => new PostBackAction( returnResource, authorizationCheckDisabledPredicate: _ => true ) ) ) ) );
		if( CookiesDisabled )
			content.Add( new Paragraph( Translation.YourBrowserHasCookiesDisabled.ToComponents() ) );
		if( ClockError.HasValue )
			content.Add( new Paragraph( AuthenticationStatics.GetClockWrongMessage( ClockError.Value ).ToComponents() ) );
		return content;
	}

	private bool verificationFailed() => parametersModification.CookiesDisabled || parametersModification.ClockError is not null;
}