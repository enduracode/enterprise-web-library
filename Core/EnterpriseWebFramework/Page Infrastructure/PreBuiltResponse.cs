using EnterpriseWebLibrary.WebSessionState;

// EwlResource

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	partial class PreBuiltResponse {
		protected override AlternativeResourceMode createAlternativeMode() =>
			!StandardLibrarySessionState.HasResponseToSend ? new DisabledResourceMode( "There is no response to send." ) : null;

		protected override UrlHandler getUrlParent() => new Admin.EntitySetup();
		protected internal override ConnectionSecurity ConnectionSecurity => ConnectionSecurity.MatchingCurrentRequest;

		protected override EwfSafeRequestHandler getOrHead() => new EwfSafeResponseWriter( new EwfResponse( StandardLibrarySessionState.ResponseToSend ) );
	}
}