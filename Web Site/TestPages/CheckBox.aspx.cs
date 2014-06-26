using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	public partial class CheckBox: EwfPage {
		protected override void loadData() {
			ph.AddControlsReturnThis(
				ControlStack.CreateWithControls( true, new EwfCheckBox( false, label: "Inline Check Box" ), new BlockCheckBox( false, label: "Block Check Box" ) ) );
		}

		public override bool IsAutoDataUpdater { get { return true; } }
	}
}