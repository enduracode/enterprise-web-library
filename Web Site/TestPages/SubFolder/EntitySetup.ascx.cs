using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.EnterpriseWebFramework.Ui.Entity;
using Tewl.Tools;

namespace EnterpriseWebLibrary.WebSite.TestPages.SubFolder {
	partial class EntitySetup: UserControl, UiEntitySetupBase {
		partial class Info {
			protected override ResourceInfo createParentResourceInfo() => ActionControls.GetInfo();

			protected override List<ResourceGroup> createResourceInfos() =>
				new List<ResourceGroup> { new ResourceGroup( new General.Info( this ), new Details.Info( this ), new Disabled.Info( this ), new New.Info( this ) ) };

			public override string EntitySetupName => "Nested";

			public override TabMode GetTabMode() => TabMode.Horizontal;
		}

		void EntitySetupBase.LoadData() {
			ph.AddControlsReturnThis( new Paragraph( "Awesome content goes here.".ToComponents() ).ToCollection().GetControls() );
		}

		IReadOnlyCollection<ActionComponentSetup> UiEntitySetupBase.GetNavActions() => Enumerable.Empty<ActionComponentSetup>().Materialize();
		IReadOnlyCollection<NavFormControl> UiEntitySetupBase.GetNavFormControls() => Enumerable.Empty<NavFormControl>().Materialize();
		IReadOnlyCollection<ActionComponentSetup> UiEntitySetupBase.GetActions() => Enumerable.Empty<ActionComponentSetup>().Materialize();
	}
}