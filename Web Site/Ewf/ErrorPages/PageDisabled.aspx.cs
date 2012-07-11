using RedStapler.StandardLibrary.DataAccess;

// Parameter: string message

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.ErrorPages {
	public partial class PageDisabled: EwfPage {
		partial class Info {
			protected override void init( DBConnection cn ) {}
			protected override bool IsIntermediateInstallationPublicPage { get { return true; } }
			protected override ConnectionSecurity ConnectionSecurity { get { return ConnectionSecurity.MatchingCurrentRequest; } }
		}

		protected override void LoadData( DBConnection cn ) {
			error.InnerText = info.Message.Length > 0 ? info.Message : Translation.ThePageYouRequestedIsDisabled;
		}
	}
}