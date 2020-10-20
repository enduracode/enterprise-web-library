using System.Collections.Generic;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.Admin {
	partial class EntitySetup: UiEntitySetup {
		protected override ResourceBase createParentResource() => null;

		protected override List<ResourceGroup> createResources() =>
			new List<ResourceGroup> { new ResourceGroup( new BasicTests.Info( this ), new RequestProfiling.Info( this ), new SystemUsers.Info( this ) ) };

		public override string EntitySetupName => "EWF Admin";

		protected override bool UserCanAccessEntitySetup {
			get {
				if( !UserManagementStatics.UserManagementEnabled )
					return true;
				return AppTools.User != null && AppTools.User.Role.CanManageUsers;
			}
		}

		EntityUiSetup UiEntitySetup.GetUiSetup() => new EntityUiSetup();
	}
}