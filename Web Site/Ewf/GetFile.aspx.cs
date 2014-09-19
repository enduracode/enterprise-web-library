using System;
using RedStapler.StandardLibrary.WebSessionState;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite {
	partial class GetFile: EwfPage {
		partial class Info {
			protected override void init() {
				if( StandardLibrarySessionState.Instance.ResponseToSend == null )
					throw new ApplicationException();
			}

			protected override ConnectionSecurity ConnectionSecurity { get { return ConnectionSecurity.MatchingCurrentRequest; } }
		}

		protected override EwfSafeResponseWriter responseWriter {
			get { return new EwfSafeResponseWriter( new EwfResponse( StandardLibrarySessionState.Instance.ResponseToSend ) ); }
		}
	}
}