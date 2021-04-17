using System;
using EnterpriseWebLibrary.WebSessionState;

// EwlPage

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	partial class PreBuiltResponse {
		protected override void init() {
			if( StandardLibrarySessionState.Instance.ResponseToSend == null )
				throw new ApplicationException();
		}

		protected internal override ConnectionSecurity ConnectionSecurity => ConnectionSecurity.MatchingCurrentRequest;

		protected override EwfSafeRequestHandler requestHandler =>
			new EwfSafeResponseWriter( new EwfResponse( StandardLibrarySessionState.Instance.ResponseToSend ) );
	}
}