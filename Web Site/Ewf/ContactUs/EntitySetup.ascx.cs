using System.Collections.Generic;
using System.Web.UI;
using RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayElements.Entity;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Ui;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.ContactUs {
	public partial class EntitySetup: UserControl, EntityDisplaySetup {
		partial class Info {
			protected override PageInfo createParentPageInfo() {
				return null;
			}

			protected override List<PageGroup> createPageInfos() {
				return new List<PageGroup>();
			}

			public override string EntitySetupName { get { return "Contact Us"; } }
			protected override bool UserCanAccessEntitySetup { get { return AppTools.User != null; } }
		}

		void EntitySetupBase.LoadData() {}

		public List<ActionButtonSetup> CreateNavButtonSetups() {
			var navButtonSetups = new List<ActionButtonSetup>();
			return navButtonSetups;
		}

		public List<LookupBoxSetup> CreateLookupBoxSetups() {
			var lookupBoxSetups = new List<LookupBoxSetup>();
			return lookupBoxSetups;
		}

		public List<ActionButtonSetup> CreateActionButtonSetups() {
			var actionButtonSetups = new List<ActionButtonSetup>();
			return actionButtonSetups;
		}
	}
}