using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.AlternativePageModes;

namespace EnterpriseWebLibrary.WebSite.TestPages.SubFolder {
	public partial class New: EwfPage {
		public partial class Info {
			protected override AlternativePageMode createAlternativeMode() {
				return new NewContentPageMode();
			}
		}
	}
}