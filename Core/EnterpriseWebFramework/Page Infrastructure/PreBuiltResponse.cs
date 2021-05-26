using System;
using EnterpriseWebLibrary.WebSessionState;

// EwlResource

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	partial class PreBuiltResponse {
		protected override void init() {
			if( StandardLibrarySessionState.Instance.ResponseToSend == null )
				throw new ApplicationException();
		}

		protected override UrlHandler getUrlParent() => new Admin.EntitySetup();
		protected internal override ConnectionSecurity ConnectionSecurity => ConnectionSecurity.MatchingCurrentRequest;

		protected override EwfSafeRequestHandler getOrHead() => new EwfSafeResponseWriter( new EwfResponse( StandardLibrarySessionState.Instance.ResponseToSend ) );
	}
}