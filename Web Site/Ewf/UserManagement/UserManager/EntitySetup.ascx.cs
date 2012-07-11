using System.Collections.Generic;
using System.Web.UI;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayElements;
using RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayElements.Entity;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.UserManagement.UserManager {
	public partial class EntitySetup: UserControl, EntityDisplaySetup {
		partial class Info {
			protected override void init( DBConnection cn ) {}

			protected override PageInfo createParentPageInfo() {
				return null;
			}

			protected override List<PageGroup> createPageInfos() {
				return new List<PageGroup> { new PageGroup( new Users.Info( this ) ) };
			}

			public override string EntitySetupName { get { return "User Management"; } }
		}

		public void LoadData( DBConnection cn ) {}

		public List<ActionButtonSetup> CreateNavButtonSetups() {
			return new List<ActionButtonSetup>();
		}

		public List<LookupBoxSetup> CreateLookupBoxSetups() {
			return new List<LookupBoxSetup>();
		}

		public List<ActionButtonSetup> CreateActionButtonSetups() {
			return new List<ActionButtonSetup>();
		}
	}
}