using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class CheckBox: EwfPage {
		protected override void loadData() {
			ph.AddControlsReturnThis(
				ControlStack.CreateWithControls(
					true,
					new EwfCheckBox( false, label: "Inline Check Box" ),
					new BlockCheckBox( false, "Block Check Box".ToComponents() ) ) );
		}

		public override bool IsAutoDataUpdater => true;
	}
}