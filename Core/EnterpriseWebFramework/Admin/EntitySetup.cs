﻿using EnterpriseWebLibrary.UserManagement;
using Serilog.Core;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Admin;

partial class EntitySetup: UiEntitySetup {
	private static Func<UrlHandler>? frameworkUrlParentGetter;

	internal static void Init( Func<UrlHandler> frameworkUrlParentGetter, LoggingLevelSwitch diagnosticLogLevelSwitch ) {
		DiagnosticLog.Init( diagnosticLogLevelSwitch );

		EntitySetup.frameworkUrlParentGetter = frameworkUrlParentGetter;
	}

	protected override ResourceParent? createParent() => null;

	protected override string getEntitySetupName() => "{0} Admin".FormatWith( EwlStatics.EwlInitialism );

	protected override bool userCanAccess {
		get {
			if( !UserManagementStatics.UserManagementEnabled )
				return true;
			return AppTools.User != null && AppTools.User.Role.CanManageUsers;
		}
	}

	public override ResourceBase DefaultResource => new BasicTests( this );

	protected override IEnumerable<ResourceGroup> createListedResources() =>
		new ResourceGroup(
			new BasicTests( this ),
			new RequestProfiling( this ),
			new DiagnosticLog( this ),
			new UserManagement( this ),
			new OpenIdProvider( this ),
			new CssElements( this ) ).ToCollection();

	protected override UrlHandler getUrlParent() => frameworkUrlParentGetter!();

	protected override UrlHandler getRequestHandler() => new BasicTests( this );

	protected override IEnumerable<UrlPattern> getChildUrlPatterns() =>
		StaticFiles.FolderSetup.UrlPatterns.Literal( "static" )
			.Append( NonLiveLogIn.UrlPatterns.Literal( "non-live-log-in" ) )
			.Append( EnterpriseWebFramework.UserManagement.Pages.LogIn.UrlPatterns.Literal( "log-in" ) )
			.Append( EnterpriseWebFramework.UserManagement.Pages.ChangePassword.UrlPatterns.Literal( "change-password" ) )
			.Append( EnterpriseWebFramework.UserManagement.SamlResources.Metadata.UrlPatterns.Literal( "saml" ) )
			.Append( EnterpriseWebFramework.UserManagement.Pages.Impersonate.UrlPatterns.Literal( "impersonate" ) )
			.Append( EnterpriseWebFramework.OpenIdProvider.Resources.EntitySetup.UrlPatterns.Literal( "oauth" ) )
			.Append( PreBuiltResponse.UrlPatterns.Literal( "pre-built-response" ) )
			.Append( ContactSupport.UrlPatterns.Literal( "contact-support" ) )
			.Append( ErrorPages.ResourceNotAvailable.UrlPatterns.Literal( "resource-not-available" ) )
			.Append( ErrorPages.AccessDenied.UrlPatterns.Literal( "access-denied" ) )
			.Append( ErrorPages.ResourceDisabled.UrlPatterns.Literal( "resource-disabled" ) )
			.Append( ErrorPages.LogInSessionExpired.UrlPatterns.Literal( "log-in-session-expired" ) )
			.Append( ErrorPages.UnhandledException.UrlPatterns.Literal( "unhandled-exception" ) )
			.Append( BasicTests.UrlPatterns.Literal( this, "tests" ) )
			.Append( RequestProfiling.UrlPatterns.Literal( this, "profiling" ) )
			.Append( DiagnosticLog.UrlPatterns.Literal( this, "log" ) )
			.Append( UserManagement.UrlPatterns.Literal( this, "users" ) )
			.Append( OpenIdProvider.UrlPatterns.Literal( this, "openid-provider" ) )
			.Append( CssElements.UrlPatterns.Literal( this, "css-elements" ) )
			.Append( ErrorLog.UrlPatterns.Literal( this, "system-error-log" ) );

	EntityUiSetup UiEntitySetup.GetUiSetup() =>
		new(
			navActionGetter:
			_ => new HyperlinkSetup( new ErrorLog( this ), "System error log", icon: new ActionComponentIcon( new FontAwesomeIcon( "fa-exclamation-triangle" ) ) )
				.ToCollection(),
			actionGetter: _ =>
				UserManagementStatics.UserManagementEnabled
					? new HyperlinkSetup(
						new EnterpriseWebFramework.UserManagement.Pages.Impersonate( PageBase.Current.GetUrl() ),
						"Impersonate user",
						icon: new ActionComponentIcon( new FontAwesomeIcon( "fa-key" ) ) ).ToCollection()
					: Array.Empty<ActionComponentSetup>() );
}