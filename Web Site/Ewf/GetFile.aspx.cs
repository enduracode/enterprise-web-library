using System;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.WebFileSending;
using RedStapler.StandardLibrary.WebSessionState;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.RedStapler.TestWebSite.Ewf {
	public partial class GetFile: EwfPage {
		partial class Info {
			protected override void init( DBConnection cn ) {
				if( StandardLibrarySessionState.Instance.FileToBeDownloaded == null )
					throw new ApplicationException();
			}

			protected override ConnectionSecurity ConnectionSecurity { get { return ConnectionSecurity.MatchingCurrentRequest; } }
		}

		protected override FileCreator fileCreator { get { return new FileCreator( delegate { return StandardLibrarySessionState.Instance.FileToBeDownloaded; } ); } }

		protected override bool sendsFileInline { get { return false; } }

		protected override void LoadData( DBConnection cn ) {}
	}
}