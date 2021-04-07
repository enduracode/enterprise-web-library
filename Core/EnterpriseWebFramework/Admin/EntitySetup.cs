using System;
using System.Collections.Generic;
using System.Linq;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Admin {
	partial class EntitySetup: UiEntitySetup {
		private static Func<UrlHandler> frameworkUrlParentGetter;

		internal static void Init( Func<UrlHandler> frameworkUrlParentGetter ) {
			EntitySetup.frameworkUrlParentGetter = frameworkUrlParentGetter;
		}

		protected override ResourceBase createParentResource() => null;

		public override string EntitySetupName => "EWF Admin";

		protected internal override bool UserCanAccessEntitySetup {
			get {
				if( !UserManagementStatics.UserManagementEnabled )
					return true;
				return AppTools.User != null && AppTools.User.Role.CanManageUsers;
			}
		}

		protected override IEnumerable<ResourceGroup> createListedResources() =>
			new ResourceGroup( new BasicTests( this ), new RequestProfiling( this ), new SystemUsers( this ) ).ToCollection();

		protected override UrlHandler getUrlParent() => frameworkUrlParentGetter();

		protected override UrlHandler getRequestHandler() => new BasicTests( this );

		protected override IEnumerable<UrlPattern> getChildUrlPatterns() {
			return new UrlPattern( /* use static as the segment to get to framework static files */ ).ToCollection()
				.Append( new UrlPattern( /* use impersonate as the segment to get to MetaLogicFactory.CreateSelectUserPageInfo( "" ) */ ) );
		}

		EntityUiSetup UiEntitySetup.GetUiSetup() => new EntityUiSetup();
	}
}