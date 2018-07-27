using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using EnterpriseWebLibrary.EnterpriseWebFramework.Ui;

// Parameter: string returnAndDestinationUrl

namespace EnterpriseWebLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.UserManagement.ChangePassword {
	partial class EntitySetup: UserControl, UiEntitySetupBase {
		partial class Info {
			protected override ResourceInfo createParentResourceInfo() {
				return null;
			}

			protected override List<ResourceGroup> createResourceInfos() {
				return new List<ResourceGroup>();
			}

			public override string EntitySetupName => "Change Password";
		}

		void EntitySetupBase.LoadData() {}

		IReadOnlyCollection<ActionComponentSetup> UiEntitySetupBase.GetNavActions() =>
			new HyperlinkSetup( new ExternalResourceInfo( info.ReturnAndDestinationUrl ), "Back" ).ToCollection();

		List<LookupBoxSetup> UiEntitySetupBase.CreateLookupBoxSetups() => new List<LookupBoxSetup>();
		IReadOnlyCollection<ActionComponentSetup> UiEntitySetupBase.GetActions() => Enumerable.Empty<ActionComponentSetup>().Materialize();
	}
}