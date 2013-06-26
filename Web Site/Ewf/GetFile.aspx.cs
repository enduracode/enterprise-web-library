using System;
using RedStapler.StandardLibrary.WebFileSending;
using RedStapler.StandardLibrary.WebSessionState;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite {
	public partial class GetFile: EwfPage {
		partial class Info {
			protected override void init() {
				if( StandardLibrarySessionState.Instance.FileToBeDownloaded == null )
					throw new ApplicationException();
			}

			protected override ConnectionSecurity ConnectionSecurity { get { return ConnectionSecurity.MatchingCurrentRequest; } }
		}

		protected override FileCreator fileCreator { get { return new FileCreator( delegate { return StandardLibrarySessionState.Instance.FileToBeDownloaded; } ); } }
		protected override bool sendsFileInline { get { return false; } }
	}
}