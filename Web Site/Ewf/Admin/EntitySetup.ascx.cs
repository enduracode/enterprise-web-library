using System.Collections.Generic;
using System.Web.UI;
using RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayElements.Entity;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Ui;
using RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.Admin {
	partial class EntitySetup: UserControl, EntityDisplaySetup {
		partial class Info {
			protected override ResourceInfo createParentResourceInfo() {
				return null;
			}

			protected override List<ResourceGroup> createResourceInfos() {
				return new List<ResourceGroup> { new ResourceGroup( new BasicTests.Info( this ), new RequestProfiling.Info( this ), new SystemUsers.Info( this ) ) };
			}

			public override string EntitySetupName { get { return "EWF Admin"; } }

			protected override bool UserCanAccessEntitySetup {
				get {
					if( !UserManagementStatics.UserManagementEnabled )
						return true;
					return AppTools.User != null && AppTools.User.Role.CanManageUsers;
				}
			}
		}

		void EntitySetupBase.LoadData() {}

		List<ActionButtonSetup> EntityDisplaySetup.CreateNavButtonSetups() {
			return new List<ActionButtonSetup>();
		}

		List<LookupBoxSetup> EntityDisplaySetup.CreateLookupBoxSetups() {
			return new List<LookupBoxSetup>();
		}

		List<ActionButtonSetup> EntityDisplaySetup.CreateActionButtonSetups() {
			return new List<ActionButtonSetup>();
		}
	}
}