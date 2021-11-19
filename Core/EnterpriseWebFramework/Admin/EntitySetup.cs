using System;
using System.Collections.Generic;
using System.Linq;
using EnterpriseWebLibrary.UserManagement;
using Humanizer;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Admin {
	partial class EntitySetup: UiEntitySetup {
		private static Func<UrlHandler> frameworkUrlParentGetter;

		internal static void Init( Func<UrlHandler> frameworkUrlParentGetter ) {
			EntitySetup.frameworkUrlParentGetter = frameworkUrlParentGetter;
		}

		protected override ResourceBase createParentResource() => null;

		public override string EntitySetupName => "{0} Admin".FormatWith( EwlStatics.EwlInitialism );

		protected internal override bool UserCanAccessEntitySetup {
			get {
				if( !UserManagementStatics.UserManagementEnabled )
					return true;
				return AppTools.User != null && AppTools.User.Role.CanManageUsers;
			}
		}

		protected override IEnumerable<ResourceGroup> createListedResources() =>
			new ResourceGroup( new BasicTests( this ), new RequestProfiling( this ), new UserManagement( this ) ).ToCollection();

		protected override UrlHandler getUrlParent() => frameworkUrlParentGetter();

		protected override UrlHandler getRequestHandler() => new BasicTests( this );

		protected override IEnumerable<UrlPattern> getChildUrlPatterns() =>
			StaticFiles.FolderSetup.UrlPatterns.Literal( "static" )
				.ToCollection()
				.Append( NonLiveLogIn.UrlPatterns.Literal( "non-live-log-in" ) )
				.Append( EnterpriseWebFramework.UserManagement.Pages.LogIn.UrlPatterns.Literal( "log-in" ) )
				.Append( EnterpriseWebFramework.UserManagement.Pages.ChangePassword.UrlPatterns.Literal( "change-password" ) )
				.Append( EnterpriseWebFramework.UserManagement.SamlLogIn.UrlPatterns.Literal( "saml-log-in" ) )
				.Append( EnterpriseWebFramework.UserManagement.SamlAssertions.UrlPatterns.Literal( "saml-assertions" ) )
				.Append( EnterpriseWebFramework.UserManagement.Pages.Impersonate.UrlPatterns.Literal( "impersonate" ) )
				.Append( PreBuiltResponse.UrlPatterns.Literal( "pre-built-response" ) )
				.Append( ContactSupport.UrlPatterns.Literal( "contact-support" ) )
				.Append( ErrorPages.AccessDenied.UrlPatterns.Literal( "access-denied" ) )
				.Append( ErrorPages.ResourceDisabled.UrlPatterns.Literal( "resource-disabled" ) )
				.Append( ErrorPages.ResourceNotAvailable.UrlPatterns.Literal( "resource-not-available" ) )
				.Append( ErrorPages.UnhandledException.UrlPatterns.Literal( "unhandled-exception" ) )
				.Append( BasicTests.UrlPatterns.Literal( this, "tests" ) )
				.Append( RequestProfiling.UrlPatterns.Literal( this, "profiling" ) )
				.Append( UserManagement.UrlPatterns.Literal( this, "users" ) )
				.Append( EnterpriseWebFramework.UserManagement.SamlMetadata.UrlPatterns.Literal( "saml" ) );

		EntityUiSetup UiEntitySetup.GetUiSetup() =>
			new EntityUiSetup(
				actions: new HyperlinkSetup(
					new EnterpriseWebFramework.UserManagement.Pages.Impersonate( PageBase.Current.GetUrl() ),
					"Impersonate user",
					icon: new ActionComponentIcon( new FontAwesomeIcon( "fa-key" ) ) ).ToCollection() );
	}
}