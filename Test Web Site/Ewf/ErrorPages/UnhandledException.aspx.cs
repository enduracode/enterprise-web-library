using RedStapler.StandardLibrary.DataAccess;

// Parameter: string dummy

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.RedStapler.TestWebSite.Ewf.ErrorPages {
	public partial class UnhandledException: EwfPage {
		partial class Info {
			protected override void init( DBConnection cn ) {}
			protected override bool IsIntermediateInstallationPublicPage { get { return true; } }
			protected override ConnectionSecurity ConnectionSecurity { get { return ConnectionSecurity.MatchingCurrentRequest; } }
		}

		protected override void LoadData( DBConnection cn ) {
			error.InnerText = Translation.AnErrorHasOccurred;

			Response.StatusCode = 500;
			Response.TrySkipIisCustomErrors = true;
		}
	}
}