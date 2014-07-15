// Parameter: string dummy

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.ErrorPages {
	public partial class UnhandledException: EwfPage {
		partial class Info {
			protected override bool IsIntermediateInstallationPublicPage { get { return true; } }
			protected override ConnectionSecurity ConnectionSecurity { get { return ConnectionSecurity.MatchingCurrentRequest; } }
		}

		protected override void loadData() {
			error.InnerText = Translation.AnErrorHasOccurred;

			Response.StatusCode = 500;
			Response.TrySkipIisCustomErrors = true;
		}
	}
}