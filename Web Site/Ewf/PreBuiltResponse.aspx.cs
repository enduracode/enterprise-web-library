using System;
using EnterpriseWebLibrary.WebSessionState;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite {
	partial class PreBuiltResponse: EwfPage {
		partial class Info {
			protected override void init() {
				if( StandardLibrarySessionState.Instance.ResponseToSend == null )
					throw new ApplicationException();
			}

			protected override ConnectionSecurity ConnectionSecurity => ConnectionSecurity.MatchingCurrentRequest;
		}

		protected override EwfSafeRequestHandler requestHandler =>
			new EwfSafeResponseWriter( new EwfResponse( StandardLibrarySessionState.Instance.ResponseToSend ) );
	}
}