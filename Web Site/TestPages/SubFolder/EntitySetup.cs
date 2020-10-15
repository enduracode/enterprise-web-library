using System.Collections.Generic;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using Tewl.Tools;

namespace EnterpriseWebLibrary.WebSite.TestPages.SubFolder {
	partial class EntitySetup: UiEntitySetup {
		protected override ResourceInfo createParentResourceInfo() => ActionControls.GetInfo();

		protected override List<ResourceGroup> createResourceInfos() =>
			new List<ResourceGroup> { new ResourceGroup( new General.Info( this ), new Details.Info( this ), new Disabled.Info( this ), new New.Info( this ) ) };

		public override string EntitySetupName => "Nested";

		EntityUiSetup UiEntitySetup.GetUiSetup() =>
			new EntityUiSetup( entitySummaryContent: new Paragraph( "Awesome content goes here.".ToComponents() ).ToCollection(), tabMode: TabMode.Horizontal );
	}
}