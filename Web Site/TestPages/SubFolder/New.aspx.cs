using EnterpriseWebLibrary.EnterpriseWebFramework;

namespace EnterpriseWebLibrary.WebSite.TestPages.SubFolder {
	partial class New: EwfPage {
		partial class Info {
			protected override AlternativeResourceMode createAlternativeMode() => new NewContentResourceMode();
		}
	}
}