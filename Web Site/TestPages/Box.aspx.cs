using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	public partial class Box: EwfPage {
		public partial class Info {
			protected override void init( DBConnection cn ) {}
		}

		protected override void LoadData( DBConnection cn ) {
			ph.AddControlsReturnThis( new RedStapler.StandardLibrary.EnterpriseWebFramework.Controls.Box( new Paragraph( "This is a basic box." ) ),
			                          new RedStapler.StandardLibrary.EnterpriseWebFramework.Controls.Box( "Heading Box", new Paragraph( "This is a box with heading." ) ) );
		}
	}
}