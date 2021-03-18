using System.Collections.Generic;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.Admin {
	partial class EntitySetup: UiEntitySetup {
		protected override ResourceBase createParentResource() => null;

		public override string EntitySetupName => "EWF Admin";

		protected override bool UserCanAccessEntitySetup {
			get {
				if( !UserManagementStatics.UserManagementEnabled )
					return true;
				return AppTools.User != null && AppTools.User.Role.CanManageUsers;
			}
		}

		protected override IEnumerable<ResourceGroup> createListedResources() =>
			new ResourceGroup( new BasicTests.Info( this ), new RequestProfiling.Info( this ), new SystemUsers.Info( this ) ).ToCollection();

		protected override UrlHandler getRequestHandler() => new BasicTests.Info( this );

		protected override IEnumerable<UrlPattern> getChildUrlPatterns() {
			return new UrlPattern( /* use impersonate as the segment to get to MetaLogicFactory.CreateSelectUserPageInfo( "" ) */ );
		}

		EntityUiSetup UiEntitySetup.GetUiSetup() => new EntityUiSetup();
	}
}