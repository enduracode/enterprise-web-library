using RedStapler.StandardLibrary.EnterpriseWebFramework;

namespace EnterpriseWebLibrary.WebSite.TestPages.SubFolder {
	partial class New: EwfPage {
		partial class Info {
			protected override AlternativeResourceMode createAlternativeMode() {
				return new NewContentResourceMode();
			}
		}
	}
}