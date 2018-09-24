using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.Admin {
	partial class EntitySetup: UserControl, UiEntitySetupBase {
		partial class Info {
			protected override ResourceInfo createParentResourceInfo() {
				return null;
			}

			protected override List<ResourceGroup> createResourceInfos() {
				return new List<ResourceGroup> { new ResourceGroup( new BasicTests.Info( this ), new RequestProfiling.Info( this ), new SystemUsers.Info( this ) ) };
			}

			public override string EntitySetupName => "EWF Admin";

			protected override bool UserCanAccessEntitySetup {
				get {
					if( !UserManagementStatics.UserManagementEnabled )
						return true;
					return AppTools.User != null && AppTools.User.Role.CanManageUsers;
				}
			}
		}

		void EntitySetupBase.LoadData() {}

		IReadOnlyCollection<ActionComponentSetup> UiEntitySetupBase.GetNavActions() => Enumerable.Empty<ActionComponentSetup>().Materialize();
		IReadOnlyCollection<NavFormControl> UiEntitySetupBase.GetNavFormControls() => Enumerable.Empty<NavFormControl>().Materialize();
		IReadOnlyCollection<ActionComponentSetup> UiEntitySetupBase.GetActions() => Enumerable.Empty<ActionComponentSetup>().Materialize();
	}
}