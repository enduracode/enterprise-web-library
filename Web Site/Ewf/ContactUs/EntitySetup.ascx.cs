using System.Collections.Generic;
using System.Web.UI;
using RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayElements.Entity;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Ui;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.ContactUs {
	partial class EntitySetup: UserControl, EntityDisplaySetup {
		partial class Info {
			protected override ResourceInfo createParentResourceInfo() {
				return null;
			}

			protected override List<ResourceGroup> createResourceInfos() {
				return new List<ResourceGroup>();
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