using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.AlternativePageModes;

namespace EnterpriseWebLibrary.WebSite.TestPages.SubFolder {
	public partial class Disabled: EwfPage {
		public partial class Info {
			protected override void init( DBConnection cn ) {}

			protected override AlternativePageMode createAlternativeMode() {
				return new DisabledPageMode( "Disabled!" );
			}
		}

		protected override void LoadData( DBConnection cn ) {}
	}
}