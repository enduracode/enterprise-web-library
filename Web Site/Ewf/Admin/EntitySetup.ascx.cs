using System.Collections.Generic;
using System.Web.UI;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayElements.Entity;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Ui;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.Admin {
	public partial class EntitySetup: UserControl, EntityDisplaySetup {
		public partial class Info {
			protected override void init( DBConnection cn ) {}

			protected override PageInfo createParentPageInfo() {
				return null;
			}

			protected override List<PageGroup> createPageInfos() {
				return new List<PageGroup> { new PageGroup( new BasicTests.Info( this ) ) };
			}

			public override string EntitySetupName { get { return "EWF Admin"; } }
		}

		public void LoadData( DBConnection cn ) {}

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