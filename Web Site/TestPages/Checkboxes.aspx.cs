using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class Checkboxes: EwfPage {
		protected override void loadData() {
			ph.AddControlsReturnThis(
				ControlStack.CreateWithControls(
					true,
					new Checkbox( false, label: "Checkbox".ToComponents() ).ToFormItem().ToControl(),
					new FlowCheckbox( false, "Flow checkbox".ToComponents() ).ToFormItem().ToControl() ) );
		}

		public override bool IsAutoDataUpdater => true;
	}
}