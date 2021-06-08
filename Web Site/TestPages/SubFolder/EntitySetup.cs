using System.Collections.Generic;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using Tewl.Tools;

namespace EnterpriseWebLibrary.WebSite.TestPages.SubFolder {
	partial class EntitySetup: UiEntitySetup {
		protected override ResourceBase createParentResource() => ActionControls.GetInfo();

		public override string EntitySetupName => "Nested";

		protected override IEnumerable<ResourceGroup> createListedResources() =>
			new List<ResourceGroup> { new ResourceGroup( new General( this ), new Details( this ), new Disabled( this ), new New( this ) ) };

		protected override UrlHandler getRequestHandler() => null;

		EntityUiSetup UiEntitySetup.GetUiSetup() =>
			new EntityUiSetup( entitySummaryContent: new Paragraph( "Awesome content goes here.".ToComponents() ).ToCollection(), tabMode: TabMode.Horizontal );
	}
}