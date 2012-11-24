using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	public partial class CheckBox: EwfPage {
		public partial class Info {
			protected override void init( DBConnection cn ) {}
		}

		protected override void LoadData( DBConnection cn ) {
			ph.AddControlsReturnThis( ControlStack.CreateWithControls( true,
			                                                           new EwfCheckBox( false, label: "Inline Check Box" ),
			                                                           new BlockCheckBox( false, label: "Block Check Box" ) ) );
		}
	}
}