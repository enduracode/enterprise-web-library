using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	public partial class StatusMessagesBasicPage: EwfPage {
		public partial class Info {
			protected override void init() {}
		}

		protected override void loadData() {
			ph.AddControlsReturnThis( EwfLink.Create( new StatusMessages.Info( es.info ), new TextActionControlStyle( "Back to EwfUi" ) ),
			                          TestPages.StatusMessages.GetTests() );
		}
	}
}