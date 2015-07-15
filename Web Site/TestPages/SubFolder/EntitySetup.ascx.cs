using System.Collections.Generic;
using System.Web.UI;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;
using EnterpriseWebLibrary.EnterpriseWebFramework.DisplayElements.Entity;
using EnterpriseWebLibrary.EnterpriseWebFramework.Ui;
using EnterpriseWebLibrary.EnterpriseWebFramework.Ui.Entity;

namespace EnterpriseWebLibrary.WebSite.TestPages.SubFolder {
	partial class EntitySetup: UserControl, EntityDisplaySetup {
		partial class Info {
			protected override ResourceInfo createParentResourceInfo() {
				return ActionControls.GetInfo();
			}

			protected override List<ResourceGroup> createResourceInfos() {
				return new List<ResourceGroup> { new ResourceGroup( new General.Info( this ), new Details.Info( this ), new Disabled.Info( this ), new New.Info( this ) ) };
			}

			public override string EntitySetupName { get { return "Nested"; } }

			public override TabMode GetTabMode() {
				return TabMode.Horizontal;
			}
		}

		void EntitySetupBase.LoadData() {
			ph.AddControlsReturnThis( new Paragraph( "Awesome content goes here." ) );
		}

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