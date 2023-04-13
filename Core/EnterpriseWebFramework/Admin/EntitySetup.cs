using EnterpriseWebLibrary.UserManagement;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Admin;

partial class EntitySetup: UiEntitySetup {
	private static Func<UrlHandler> frameworkUrlParentGetter;

	internal static void Init( Func<UrlHandler> frameworkUrlParentGetter ) {
		EntitySetup.frameworkUrlParentGetter = frameworkUrlParentGetter;
	}

	protected override ResourceParent createParent() => null;

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
		new ResourceGroup( new BasicTests( this ), new RequestProfiling( this ), new UserManagement( this ), new CssElements( this ) ).ToCollection();

	protected override UrlHandler getUrlParent() => frameworkUrlParentGetter();

	protected override UrlHandler getRequestHandler() => new BasicTests( this );

	protected override IEnumerable<UrlPattern> getChildUrlPatterns() =>
		StaticFiles.FolderSetup.UrlPatterns.Literal( "static" )
			.ToCollection()
			.Append( NonLiveLogIn.UrlPatterns.Literal( "non-live-log-in" ) )
			.Append( EnterpriseWebFramework.UserManagement.Pages.LogIn.UrlPatterns.Literal( "log-in" ) )
			.Append( EnterpriseWebFramework.UserManagement.Pages.ChangePassword.UrlPatterns.Literal( "change-password" ) )
			.Append( EnterpriseWebFramework.UserManagement.SamlResources.Metadata.UrlPatterns.Literal( "saml" ) )
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
			.Append( CssElements.UrlPatterns.Literal( this, "css-elements" ) );

	EntityUiSetup UiEntitySetup.GetUiSetup() =>
		new(
			actionGetter: _ => new HyperlinkSetup(
				new EnterpriseWebFramework.UserManagement.Pages.Impersonate( PageBase.Current.GetUrl() ),
				"Impersonate user",
				icon: new ActionComponentIcon( new FontAwesomeIcon( "fa-key" ) ) ).ToCollection() );
}