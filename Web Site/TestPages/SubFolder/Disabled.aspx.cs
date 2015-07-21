using EnterpriseWebLibrary.EnterpriseWebFramework;

namespace EnterpriseWebLibrary.WebSite.TestPages.SubFolder {
	partial class Disabled: EwfPage {
		partial class Info {
			protected override AlternativeResourceMode createAlternativeMode() {
				return new DisabledResourceMode( "Disabled!" );
			}
		}
	}
}