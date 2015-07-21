// Parameter: string message

namespace EnterpriseWebLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.ErrorPages {
	partial class PageDisabled: EwfPage {
		partial class Info {
			protected override bool IsIntermediateInstallationPublicResource { get { return true; } }
			protected override ConnectionSecurity ConnectionSecurity { get { return ConnectionSecurity.MatchingCurrentRequest; } }
		}

		protected override void loadData() {
			error.InnerText = info.Message.Length > 0 ? info.Message : Translation.ThePageYouRequestedIsDisabled;
		}
	}
}