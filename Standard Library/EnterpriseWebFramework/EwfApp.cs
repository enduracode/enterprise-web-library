using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using RedStapler.StandardLibrary.Configuration;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Ui;
using RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement;
using StackExchange.Profiling;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The HttpApplication class for a web site using EWF. Provides access to the authenticated user, handles errors, and performs other useful functions.
	/// </summary>
	public abstract class EwfApp: HttpApplication {
		private static bool ewlInitialized;
		private static bool initialized;
		private static Timer initFailureUnloadTimer;
		private static WebApplication webAppConfiguration;
		internal static Type GlobalType { get; private set; }
		internal static AppMetaLogicFactory MetaLogicFactory { get; private set; }

		// This member is per web user (request). We must be careful to never accidentally use values from a previous request.
		internal AppRequestState RequestState { get; private set; }

		/// <summary>
		/// Returns the EwfApp object for the current HTTP context.
		/// </summary>
		public static EwfApp Instance { get { return HttpContext.Current == null ? null : HttpContext.Current.ApplicationInstance as EwfApp; } }

		/// <summary>
		/// Registers event handlers for certain application events.
		/// </summary>
		protected EwfApp() {
			BeginRequest += handleBeginRequest;
			AuthenticateRequest += handleAuthenticateRequest;
			PostAuthenticateRequest += handlePostAuthenticateRequest;
			EndRequest += handleEndRequest;
			Error += handleError;
		}

		/// <summary>
		/// Call this from Application_Start in your Global.asax.cs file. Besides this call, there should be no other code in the method.
		/// </summary>
		// We could save people the effort of calling this by using trick #1 in
		// http://www.paraesthesia.com/archive/2011/02/08/dynamic-httpmodule-registration-in-asp-net-4-0.aspx, but that would probably require making this a static
		// method and would probably also cause this method to run at start up time in *all* web applications that reference the Standard Library, even the ones
		// that don't want to use EWF.
		protected void ewfApplicationStart( SystemLogic systemLogic ) {
			// This is a hack to support data-access state in WCF services.
			var wcfDataAccessState = new ThreadLocal<DataAccessState>( () => new DataAccessState() );

			// Initialize system.
			var initTimeDataAccessState = new ThreadLocal<DataAccessState>( () => new DataAccessState() );
			try {
				AppTools.Init(
					Path.GetFileName( Path.GetDirectoryName( HttpRuntime.AppDomainAppPath ) ),
					false,
					systemLogic,
					mainDataAccessStateGetter: () => {
						// We must use the Instance property here to prevent this logic from always returning the request state of the *first* EwfApp instance.
						return Instance != null
							       ? Instance.RequestState != null ? Instance.RequestState.DataAccessState : initTimeDataAccessState.Value
							       : System.ServiceModel.OperationContext.Current != null ? wcfDataAccessState.Value : null;
					} );
			}
			catch {
				// Suppress all exceptions since there is no way to report them.
				return;
			}
			ewlInitialized = true;

			// Initialize web application.
			if( !AppTools.SecondaryInitFailed ) {
				executeWithBasicExceptionHandling(
					() => {
						webAppConfiguration = AppTools.InstallationConfiguration.WebApplications.Single( a => a.Name == AppTools.AppName );

						// Prevent MiniProfiler JSON exceptions caused by pages with hundreds of database queries.
						MiniProfiler.Settings.MaxJsonResponseSize = int.MaxValue;

						GlobalType = GetType().BaseType;
						MetaLogicFactory =
							GlobalType.Assembly.CreateInstance( "RedStapler.StandardLibrary.EnterpriseWebFramework." + GlobalType.Namespace + ".MetaLogicFactory" ) as
							AppMetaLogicFactory;
						if( MetaLogicFactory == null )
							throw new ApplicationException( "Meta logic factory not found." );

						// This initialization could be performed using reflection. There is no need for EwfApp to have a dependency on these classes.
						if( systemLogic != null )
							CssPreprocessingStatics.Init( systemLogic.GetType().Assembly, GlobalType.Assembly );
						else
							CssPreprocessingStatics.Init( GlobalType.Assembly );
						EwfUiStatics.Init( GlobalType );

						initializeWebApp();

						initTimeDataAccessState = null;
						initialized = true;
					},
					false,
					false );
			}

			// If initialization failed, unload and restart the application after a reasonable delay.
			if( !initialized ) {
				const int unloadDelay = 60000; // milliseconds
				initFailureUnloadTimer = new Timer(
					state => executeWithBasicExceptionHandling(
						() => {
							if( AppTools.IsDevelopmentInstallation )
								return;
							HttpRuntime.UnloadAppDomain();

							// Restart the application by making a request. Idea from Rick Strahl:
							// http://weblog.west-wind.com/posts/2013/Oct/02/Use-IIS-Application-Initialization-for-keeping-ASPNET-Apps-alive.
							//
							// Disable server certificate validation so that this request gets through even for web sites that don't use a certificate that is trusted by
							// default. There is no security risk since we're not sending any sensitive information and we're not using the response.
							NetTools.ExecuteWithResponse( IisConfigurationStatics.GetFirstBaseUrlForCurrentSite( false ), response => { }, disableCertificateValidation: true );
						},
						false,
						false ),
					null,
					unloadDelay,
					Timeout.Infinite );
			}
		}

		/// <summary>
		/// Performs web-site specific initialization. Called at the end of ewfApplicationStart.
		/// </summary>
		protected abstract void initializeWebApp();

		/// <summary>
		/// Standard library use only.
		/// </summary>
		public static bool SupportsSecureConnections { get { return webAppConfiguration.SupportsSecureConnections || AppTools.IsIntermediateInstallation; } }

		internal static string GetDefaultBaseUrl( bool secure ) {
			return webAppConfiguration.DefaultBaseUrl.GetUrlString( secure );
		}

		private void handleBeginRequest( object sender, EventArgs e ) {
			if( !initialized ) {
				// We can't redirect to a normal page to communicate this information because since initialization failed, the request for that page will trigger
				// another BeginRequest event that puts us in an infinite loop. We can't rely on anything except an HTTP return code. Suppress exceptions; there is no
				// way to report them since even our basic exception handling may not work if the application isn't initialized.
				try {
					set500StatusCode( "Initialization Failure" );
				}
				catch {}
				return;
			}

			executeWithBasicExceptionHandling(
				delegate {
					if( RequestState != null ) {
						RequestState = null;
						throw new ApplicationException( "AppRequestState was not properly cleaned up from a previous request." );
					}


					// This used to be just HttpContext.Current.Request.Url, but that doesn't work with Azure due to the use of load balancing. An Azure load balancer
					// will bind to the ip/host/port through which all web requests should come in, and then the request is redirected to one of the server instances
					// running this web application. For example, a user will request mydomain.com. In Azure, there may be two instances running this web site, on
					// someIp:81 and someIp:82. For some reason, HttpContext.Current.Request.Url ends up using the host and port from one of these addresses instead of
					// using the host and port from the HTTP Host header, which is what the client is actually "viewing". Basically, HttpContext.Current.Request.Url
					// returns http://someIp:81?something=1 instead of http://mydomain.com?something=1. See
					// http://stackoverflow.com/questions/9560838/azure-load-balancer-causes-400-error-invalid-hostname-on-postback.

					var baseUrl = getRequestBaseUrl( Request );
					if( !baseUrl.Any() ) {
						setStatusCode( 400 );
						return;
					}
					var appRelativeUrl = GetRequestAppRelativeUrl( Request, disableLeadingSlashRemoval: true );

					// If the base URL doesn't include a path and the app-relative URL is just a slash, don't include this trailing slash in the URL since it will not be
					// present in the canonical URLs that we construct and therefore it would cause problems with URL normalization.
					var url = !getRequestBasePath( Request ).Any() && appRelativeUrl.Length == "/".Length ? baseUrl : baseUrl + appRelativeUrl;


					// This blocks until the entire request has been received from the client.
					// This won't compile unless it is assigned to something, which is why it is unused.
					var stream = Request.InputStream;

					RequestState = new AppRequestState( url );
				},
				false,
				true );
		}

		private string getRequestBaseUrl( HttpRequest request ) {
			var host = getRequestHost( request );
			return host.Any() ? BaseUrl.GetUrlString( RequestIsSecure( request ), host, getRequestBasePath( request ) ) : "";
		}

		/// <summary>
		/// Returns true if the specified request is secure. Override this to be more than just <see cref="HttpRequest.IsSecureConnection"/> if you are using a
		/// reverse proxy to perform SSL termination. Remember that your implementation should support not just live installations, but also development and
		/// intermediate installations.
		/// </summary>
		protected internal virtual bool RequestIsSecure( HttpRequest request ) {
			return request.IsSecureConnection;
		}

		/// <summary>
		/// Returns the host name for the specified request. Override this if you are using a reverse proxy that is changing the Host header. Include the port
		/// number in the return value if it is not the default port. Never return null. If the host name is unavailable (i.e. the request uses HTTP 1.0 and does
		/// not include a Host header), return the empty string, which will cause a 400 status code to be returned. Remember that your implementation should support
		/// not just live installations, but also development and intermediate installations.
		/// </summary>
		protected virtual string getRequestHost( HttpRequest request ) {
			var host = request.Headers[ "Host" ]; // returns null if field missing
			return host ?? "";
		}

		/// <summary>
		/// Returns the base path for the specified request. Override this if you are using a reverse proxy and are changing the base path. Never return null.
		/// Return the empty string to represent the root path. Remember that your implementation should support not just live installations, but also development
		/// and intermediate installations.
		/// </summary>
		protected virtual string getRequestBasePath( HttpRequest request ) {
			return request.RawUrl.Truncate( HttpRuntime.AppDomainAppVirtualPath.Length ).Substring( "/".Length );
		}

		private void handleAuthenticateRequest( object sender, EventArgs e ) {
			RequestState.IntermediateUserExists = IntermediateAuthenticationMethods.CookieExists();
		}

		private void handlePostAuthenticateRequest( object sender, EventArgs e ) {
			RequestState.EnableUser();
			using( MiniProfiler.Current.Step( "EWF - Get shortcut URL resolvers" ) )
				rewritePathIfShortcutUrl();
		}

		private void rewritePathIfShortcutUrl() {
			var url = GetRequestAppRelativeUrl( Request );

			var ewfResolver = new ShortcutUrlResolver(
				"ewf",
				ConnectionSecurity.SecureIfPossible,
				() => {
					var page = MetaLogicFactory.CreateBasicTestsPageInfo();
					return page.UserCanAccessResource ? page : null;
				} );

			foreach( var resolver in ewfResolver.ToSingleElementArray().Concat( GetShortcutUrlResolvers() ) ) {
				if( resolver.ShortcutUrl.ToLower() != url.ToLower() )
					continue;

				// Redirect to the same shortcut URL to fix the connection security, normalize the base URL, normalize the shortcut URL casing, or any combination of
				// these.
				var canonicalAbsoluteUrl = GetDefaultBaseUrl( resolver.ConnectionSecurity.ShouldBeSecureGivenCurrentRequest( false ) ) +
				                           resolver.ShortcutUrl.PrependDelimiter( "/" );
				if( canonicalAbsoluteUrl != RequestState.Url )
					NetTools.Redirect( canonicalAbsoluteUrl );

				if( AppTools.IsIntermediateInstallation && !RequestState.IntermediateUserExists )
					throw new AccessDeniedException( true, null );

				var resource = resolver.Function();
				if( resource == null )
					throw new AccessDeniedException( false, resolver.LogInPageGetter != null ? resolver.LogInPageGetter() : null );
				if( resource is ExternalResourceInfo )
					NetTools.Redirect( resource.GetUrl() );
				HttpContext.Current.RewritePath( getTransferPath( resource ), false );
				break;
			}
		}

		// One difference between this and HttpRequest.AppRelativeCurrentExecutionFilePath is that the latter does not include the query string.
		internal static string GetRequestAppRelativeUrl( HttpRequest request, bool disableLeadingSlashRemoval = false ) {
			// We'd like to use HttpRequest.RawUrl instead, but we can't until we eliminate our usage of HttpServerUtility.TransferRequest, which we should be able to
			// do when we've completed the transition away from Web Forms. Url.PathAndQuery changes after TransferRequest; RawUrl doesn't.
			var url = request.Url.PathAndQuery;

			// If a base path exists (on the web server, not the reverse proxy), remove it along with the subsequent slash if one exists. Otherwise just remove the
			// leading slash, which we know exists since an HTTP request path must start with a slash. We're doing this slash removal ultimately because we are trying
			// to normalize the difference between root applications and subdirectory applications by not distinguishing between app-relative URLs of "" and "/". In
			// root applications this distinction doesn't exist. We've decided on a standard of never allowing an app-relative URL of "/".
			if( HttpRuntime.AppDomainAppVirtualPath.Substring( "/".Length ).Any() ) {
				url = url.Substring( HttpRuntime.AppDomainAppVirtualPath.Length );
				url = url.StartsWith( "/" ) && !disableLeadingSlashRemoval ? url.Substring( 1 ) : url;
			}
			else if( !disableLeadingSlashRemoval )
				url = url.Substring( 1 );

			return url;
		}

		/// <summary>
		/// Gets the shortcut URL resolvers for the application.
		/// </summary>
		protected internal abstract IEnumerable<ShortcutUrlResolver> GetShortcutUrlResolvers();

		/// <summary>
		/// Gets the URL to be used for Chrome Application shortcuts. Never returns null.
		/// </summary>
		public virtual string FaviconPng48X48Url { get { return ""; } }

		/// <summary>
		/// Gets the favicon URL. Never returns null. See http://en.wikipedia.org/wiki/Favicon.
		/// </summary>
		public virtual string FaviconUrl { get { return ""; } }

		/// <summary>
		/// Gets the Typekit Kit ID. Never returns null.
		/// </summary>
		public virtual string TypekitId { get { return ""; } }

		/// <summary>
		/// Creates and returns a list of custom style sheets that should be used on all EWF pages, including those not using the EWF user interface.
		/// </summary>
		protected internal virtual List<ResourceInfo> GetStyleSheets() {
			return new List<ResourceInfo>();
		}

		/// <summary>
		/// Gets the Google Analytics Web Property ID, which should always start with "UA-". Never returns null.
		/// </summary>
		public virtual string GoogleAnalyticsWebPropertyId { get { return ""; } }

		/// <summary>
		/// Creates and returns a list of URLs for JavaScript files that should be included on all EWF pages, including those not using the EWF user interface.
		/// </summary>
		public virtual List<string> GetJavaScriptFileUrls() {
			return new List<string>();
		}

		/// <summary>
		/// Gets the display name of the application, which will be included in the title of all pages.
		/// </summary>
		public virtual string AppDisplayName { get { return ""; } }

		/// <summary>
		/// Gets the function call that should be executed when the jQuery document ready event is fired for any page in the application.
		/// </summary>
		protected internal virtual string JavaScriptDocumentReadyFunctionCall { get { return ""; } }

		/// <summary>
		/// Standard library use only.
		/// </summary>
		public void SendHealthCheck() {
			StandardLibraryMethods.SendHealthCheckEmail( AppTools.InstallationConfiguration.FullShortName + " - " + AppTools.AppName );
		}

		/// <summary>
		/// Executes all data modifications that happen simply because of a request and require no other action by the user.
		/// </summary>
		public virtual void ExecuteInitialRequestDataModifications() {}

		/// <summary>
		/// Returns true if Spanish should be used. Default is false.
		/// </summary>
		public virtual bool UseSpanishLanguage { get { return false; } }

		private void handleEndRequest( object sender, EventArgs e ) {
			if( !initialized || RequestState == null )
				return;

			executeWithBasicExceptionHandling(
				delegate {
					try {
						// This 404 condition covers two types of requests:
						// 1. Requests where we set the status code in handleError
						// 2. Requests to handlers that set the status code directly instead of throwing exceptions, e.g. the IIS static file handler
						if( Response.StatusCode == 404 && !handleErrorIfOnErrorPage( "A status code of 404 was produced", null ) )
							transferRequest( getErrorPage( MetaLogicFactory.CreatePageNotAvailableErrorPageInfo( !RequestState.HomeUrlRequest ) ), false );

						if( RequestState.TransferRequestPath.Length > 0 )
							// NOTE: If we transfer to a path with no query string, TransferRequest adds the current query string. Because of this bug we need to make sure all
							// pages we transfer to have at least one parameter.
							Server.TransferRequest( RequestState.TransferRequestPath, false, "GET", null );
					}
					catch {
						RequestState.RollbackDatabaseTransactions();
						DataAccessState.Current.ResetCache();
						throw;
					}
				},
				true,
				true );

			// Do not set a status code since we may have already set one or set a redirect page.
			executeWithBasicExceptionHandling( delegate { RequestState.CleanUp(); }, false, false );
			RequestState = null;
		}

		/// <summary>
		/// This method will not work properly unless the application is initialized, RequestState is not null, and the authenticated user is available.
		/// </summary>
		private void handleError( object sender, EventArgs e ) {
			// The reason we don't write our error page HTML directly to the response, like ASP.NET does, is that this limits the functionality of the error pages
			// and requires them to be built differently than all other pages. We don't transfer to the error pages either, because application errors can occur at
			// any time in the ASP.NET life cycle and transferring to a page causes it to immediately execute even if it's not the normal time for this to happen.
			// Redirecting works, but has the drawback of not being able to send proper HTTP error codes in the response since the redirects themselves require a
			// particular code. TransferRequest seems to be the only method that gives us everything we want.

			executeWithBasicExceptionHandling(
				delegate {
					// This code should happen first to prevent errors from going to the Windows event log.
					var exception = Server.GetLastError();
					Server.ClearError();

					RequestState.RollbackDatabaseTransactions();
					DataAccessState.Current.ResetCache();

					var errorIsWcf404 = exception.InnerException is System.ServiceModel.EndpointNotFoundException;

					// We can remove this as soon as requesting a URL with a vertical pipe doesn't blow up our web applications.
					var argException = exception as ArgumentException;
					var errorIsBogusPathException = argException != null && argException.Message == "Illegal characters in path.";

					// In the first part of this condition we check to make sure the base exception is also an HttpException, because we had a problem with WCF wrapping an
					// important non-HttpException inside an HttpException that somehow had a code of 404. In the second part of the condition (after the OR) we use
					// InnerException instead of GetBaseException because the ResourceNotAvailableException always has an inner exception that describes the specific
					// problem that occurred. The third part of the condition handles ResourceNotAvailableExceptions from HTTP handlers such as CssHandler; these are not
					// wrapped with another exception.
					if( ( exception is HttpException && ( exception as HttpException ).GetHttpCode() == 404 && exception.GetBaseException() is HttpException ) ||
					    exception.InnerException is ResourceNotAvailableException || exception is ResourceNotAvailableException || onErrorProneAspNetHandler || errorIsWcf404 ||
					    errorIsBogusPathException ) {
						setStatusCode( 404 );
						return;
					}

					if( !handleErrorIfOnErrorPage( "An exception occurred", exception ) ) {
						var accessDeniedException = exception.GetBaseException() as AccessDeniedException;
						var pageDisabledException = exception.GetBaseException() as PageDisabledException;
						if( accessDeniedException != null ) {
							if( accessDeniedException.CausedByIntermediateUser )
								transferRequest( MetaLogicFactory.GetIntermediateLogInPageInfo( RequestState.Url ), true );
							else {
								if( RequestState.UserAccessible && AppTools.User == null && UserManagementStatics.UserManagementEnabled && FormsAuthStatics.FormsAuthEnabled ) {
									if( accessDeniedException.LogInPage != null ) {
										// We pass false here to avoid complicating things with ThreadAbortExceptions.
										Response.Redirect( accessDeniedException.LogInPage.GetUrl(), false );

										CompleteRequest();
									}
									else
										transferRequest( MetaLogicFactory.GetLogInPageInfo( RequestState.Url ), true );
								}
								else
									transferRequest( getErrorPage( MetaLogicFactory.CreateAccessDeniedErrorPageInfo( !RequestState.HomeUrlRequest ) ), true );
							}
						}
						else if( pageDisabledException != null )
							transferRequest( MetaLogicFactory.CreatePageDisabledErrorPageInfo( pageDisabledException.Message ), true );
						else {
							RequestState.SetError( "", exception );
							transferRequest( getErrorPage( MetaLogicFactory.CreateUnhandledExceptionErrorPageInfo() ), true );
						}
					}
				},
				true,
				true );
		}

		private void executeWithBasicExceptionHandling( Action handler, bool setErrorInRequestState, bool set500StatusCode ) {
			try {
				handler();
			}
			catch( Exception e ) {
				// Suppress all exceptions since there is no way to report them and in some cases they could wreck the control flow for the request.
				try {
					StandardLibraryMethods.CallEveryMethod(
						delegate {
							const string prefix = "An exception occurred that could not be handled by the main exception handler:";
							if( setErrorInRequestState )
								RequestState.SetError( prefix, e );
							else
								AppTools.EmailAndLogError( prefix, e );
						},
						delegate {
							if( set500StatusCode )
								this.set500StatusCode( "Exception" );
						} );
				}
				catch {}
			}
		}

		private bool onErrorProneAspNetHandler {
			get {
				var errorProneHandlers = new[] { "~/WebResource.axd", "~/ScriptResource.axd" };
				return errorProneHandlers.SingleOrDefault( s => s.ToLower() == Request.AppRelativeCurrentExecutionFilePath.ToLower() ) != null;
			}
		}

		private void setStatusCode( int code ) {
			Response.StatusCode = code;
			CompleteRequest();
		}

		private bool handleErrorIfOnErrorPage( string errorEvent, Exception exception ) {
			// The order of the two conditions is important.
			return handleErrorIfOnUnhandledExceptionPage( errorEvent, exception ) || handleErrorIfOnHandledErrorPage( errorEvent, exception );
		}

		private bool handleErrorIfOnUnhandledExceptionPage( string errorEvent, Exception exception ) {
			if( getErrorPage( MetaLogicFactory.CreateUnhandledExceptionErrorPageInfo() ).GetUrl() != RequestState.Url )
				return false;
			RequestState.SetError( errorEvent + " during a request for the unhandled exception page" + ( exception != null ? ":" : "." ), exception );
			set500StatusCode( "Unhandled Exception Page Error" );
			return true;
		}

		private void set500StatusCode( string description ) {
			Response.StatusCode = 500;
			Response.StatusDescription = description + " in EWF Application";
			CompleteRequest();
		}

		private bool handleErrorIfOnHandledErrorPage( string errorEvent, Exception exception ) {
			var handledErrorPages = new List<PageInfo>
				{
					MetaLogicFactory.CreateAccessDeniedErrorPageInfo( false ),
					MetaLogicFactory.CreatePageDisabledErrorPageInfo( "" ),
					MetaLogicFactory.CreatePageNotAvailableErrorPageInfo( false )
				};
			if( handledErrorPages.All( p => getErrorPage( p ).GetUrl().Separate( "?", false ).First() != RequestState.Url.Separate( "?", false ).First() ) )
				return false;
			RequestState.SetError( errorEvent + " during a request for a handled error page" + ( exception != null ? ":" : "." ), exception );
			transferRequest( getErrorPage( MetaLogicFactory.CreateUnhandledExceptionErrorPageInfo() ), true );
			return true;
		}

		private void transferRequest( PageInfo page, bool completeRequest ) {
			var pageUrl = getTransferPath( page );

			// We can't immediately call TransferRequest because of a problem with session state described by Luis Abreu:
			// http://msmvps.com/blogs/luisabreu/archive/2007/10/09/are-you-using-the-new-transferrequest.aspx
			RequestState.TransferRequestPath = pageUrl;

			if( completeRequest )
				CompleteRequest();
		}

		private string getTransferPath( ResourceInfo resource ) {
			var url = resource.GetUrl( true, true, false );
			if( resource.ShouldBeSecureGivenCurrentRequest != RequestIsSecure( Request ) )
				throw new ApplicationException( url + " has a connection security setting that is incompatible with the current request." );
			return url;
		}

		private PageInfo getErrorPage( PageInfo ewfErrorPage ) {
			return errorPage ?? ewfErrorPage;
		}

		/// <summary>
		/// Gets the page that users will be redirected to when errors occur in the application.
		/// </summary>
		protected virtual PageInfo errorPage { get { return null; } }

		/// <summary>
		/// Call this from Application_End in your Global.asax.cs file. Besides this call, there should be no other code in the method.
		/// </summary>
		protected void ewfApplicationEnd() {
			if( !ewlInitialized )
				return;
			AppTools.CleanUp();

			if( !initialized ) {
				var waitHandle = new ManualResetEvent( false );
				initFailureUnloadTimer.Dispose( waitHandle );
				waitHandle.WaitOne();
			}
		}
	}
}