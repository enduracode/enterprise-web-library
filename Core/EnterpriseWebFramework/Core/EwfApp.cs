using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.SessionState;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.Email;
using EnterpriseWebLibrary.EnterpriseWebFramework.ErrorPages;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement.Pages;
using EnterpriseWebLibrary.UserManagement;
using StackExchange.Profiling;
using Tewl.Tools;
using static Humanizer.StringExtensions;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The HttpApplication class for a web site using EWF. Provides access to the authenticated user, handles errors, and performs other useful functions.
	/// </summary>
	public abstract class EwfApp: HttpApplication {
		/// <summary>
		/// EwfInitializationOps and private use only.
		/// </summary>
		internal static bool FrameworkInitialized { get; set; }

		private class HandlerAdapter: IHttpHandler {
			private readonly BasicUrlHandler handler;

			public HandlerAdapter( BasicUrlHandler handler ) {
				this.handler = handler;
			}

			void IHttpHandler.ProcessRequest( HttpContext context ) => handler.HandleRequest( context );
			bool IHttpHandler.IsReusable => false;
		}

		internal static void ExecuteWithBasicExceptionHandling( Action handler, bool addErrorToRequestState, bool set500StatusCode ) {
			try {
				handler();
			}
			catch( Exception e ) {
				// Suppress all exceptions since there is no way to report them and in some cases they could wreck the control flow for the request.
				try {
					EwlStatics.CallEveryMethod(
						delegate {
							const string prefix = "An exception occurred that could not be handled by the main exception handler:";
							if( addErrorToRequestState )
								Instance.RequestState.AddError( prefix, e );
							else
								TelemetryStatics.ReportError( prefix, e );
						},
						delegate {
							if( set500StatusCode )
								Instance.set500StatusCode( "Exception" );
						} );
				}
				catch {}
			}
		}

		// This member is per web user (request). We must be careful to never accidentally use values from a previous request.
		internal AppRequestState RequestState { get; private set; }

		/// <summary>
		/// Returns the EwfApp object for the current HTTP context.
		/// </summary>
		public static EwfApp Instance => NetTools.IsWebApp() ? HttpContext.Current.ApplicationInstance as EwfApp : null;

		/// <summary>
		/// Registers event handlers for certain application events.
		/// </summary>
		protected EwfApp() {
			BeginRequest += handleBeginRequest;
			AuthenticateRequest += handleAuthenticateRequest;
			PostAuthenticateRequest += handlePostAuthenticateRequest;
			PostResolveRequestCache += handlePostResolveRequestCache;
			MapRequestHandler += handleMapRequestHandler;
			EndRequest += handleEndRequest;
			Error += handleError;
		}

		private void handleBeginRequest( object sender, EventArgs e ) {
			if( !FrameworkInitialized ) {
				// We can't redirect to a normal page to communicate this information because since initialization failed, the request for that page will trigger
				// another BeginRequest event that puts us in an infinite loop. We can't rely on anything except an HTTP return code. Suppress exceptions; there is no
				// way to report them since even our basic exception handling may not work if the application isn't initialized.
				try {
					set500StatusCode( "Initialization Failure" );
					CompleteRequest();
				}
				catch {}
				return;
			}

			ExecuteWithBasicExceptionHandling(
				() => {
					try {
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
							Response.StatusCode = 400;
							CompleteRequest();
							return;
						}
						var appRelativeUrl = GetRequestAppRelativeUrl( Request, disableLeadingSlashRemoval: true );

						// If the base URL doesn't include a path and the app-relative URL is just a slash, don't include this trailing slash in the URL since it will not be
						// present in the canonical URLs that we construct and therefore it would cause problems with URL normalization.
						var url = !getRequestBasePath( Request ).Any() && appRelativeUrl.Length == "/".Length ? baseUrl : baseUrl + appRelativeUrl;


						// This blocks until the entire request has been received from the client.
						// This won't compile unless it is assigned to something, which is why it is unused.
						var stream = Request.InputStream;

						RequestState = new AppRequestState( url, baseUrl );
					}
					catch {
						CompleteRequest();
						throw;
					}
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
			RequestState.IntermediateUserExists = NonLiveInstallationStatics.IntermediateAuthenticationCookieExists();
		}

		private void handlePostAuthenticateRequest( object sender, EventArgs e ) {
			RequestState.EnableUser();
		}

		private void handlePostResolveRequestCache( object sender, EventArgs e ) {
			using( MiniProfiler.Current.Step( "EWF - Resolve URL" ) )
				resolveUrl();
		}

		private void resolveUrl() {
			var handlers = RequestState.ExecuteWithUserDisabled(
				() => {
					try {
						return UrlHandlingStatics.ResolveUrl( RequestState.BaseUrl, GetRequestAppRelativeUrl( Request ) );
					}
					catch( UnresolvableUrlException e ) {
						throw new ResourceNotAvailableException( "Failed to resolve the URL.", e );
					}
				} );
			if( handlers != null ) {
				// Before URL normalization, multiple copies of the same handler can exist in the list. When a new handler object is created and it matches more than
				// one handler in the list, we want parameters to be taken from the lowest-level segment. That’s why we reverse the handlers here.
				RequestState.SetUrlHandlers( handlers.Reverse().Materialize() );

				HttpContext.Current.RemapHandler( new HandlerAdapter( handlers.Last() ) );
				if( handlers.Last() is PageBase || handlers.Last() is EntitySetupBase || handlers.Last() is PreBuiltResponse )
					HttpContext.Current.SetSessionStateBehavior( SessionStateBehavior.Required );
				return;
			}

			// ACME challenge response; see https://tools.ietf.org/html/rfc8555#section-8.3
			var absoluteUrl = new Uri( RequestState.Url );
			if( absoluteUrl.Scheme == "http" && absoluteUrl.Port == 80 && absoluteUrl.AbsolutePath.StartsWith( "/.well-known/acme-challenge/" ) ) {
				var systemManager = ConfigurationStatics.MachineConfiguration?.SystemManager;
				if( systemManager != null ) {
					ResourceBase.WriteRedirectResponse(
						HttpContext.Current,
						systemManager.HttpBaseUrl.Replace( "https://", "http://" ) +
						"/Pages/Public/AcmeChallengeResponse.aspx;token={0}".FormatWith( HttpUtility.UrlEncode( absoluteUrl.Segments.Last() ) ),
						false );
					CompleteRequest();
				}
			}
		}

		// One difference between this and HttpRequest.AppRelativeCurrentExecutionFilePath is that the latter does not include the query string.
		internal static string GetRequestAppRelativeUrl( HttpRequest request, bool disableLeadingSlashRemoval = false ) {
			// See https://stackoverflow.com/a/782002/35349.
			var url = request.ServerVariables[ "HTTP_URL" ];

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
		/// Returns the base URL patterns for the application.
		/// </summary>
		protected internal abstract IEnumerable<BaseUrlPattern> GetBaseUrlPatterns();

		private void handleMapRequestHandler( object sender, EventArgs e ) {
			if( HttpContext.Current.Handler == null )
				throw new ResourceNotAvailableException( "Failed to resolve the URL.", null );
		}

		/// <summary>
		/// Standard library use only.
		/// </summary>
		public void SendHealthCheck() {
			EmailStatics.SendHealthCheckEmail( ConfigurationStatics.InstallationConfiguration.FullShortName + " - " + ConfigurationStatics.AppName );
		}

		/// <summary>
		/// Gets the Internet media type overrides for the application, which are used when serving static files. Do not return null.
		/// </summary>
		protected internal virtual IEnumerable<MediaTypeOverride> GetMediaTypeOverrides() {
			return new MediaTypeOverride[ 0 ];
		}

		/// <summary>
		/// Returns true if Spanish should be used. Default is false.
		/// </summary>
		public virtual bool UseSpanishLanguage => false;

		private void handleEndRequest( object sender, EventArgs e ) {
			if( !FrameworkInitialized || RequestState == null )
				return;

			// Do not set a status code since we may have already set one or set a redirect page.
			ExecuteWithBasicExceptionHandling( () => RequestState.CleanUp(), false, false );
			RequestState = null;
		}

		/// <summary>
		/// This method will not work properly unless the application is initialized, RequestState is not null, and the authenticated user is available.
		/// </summary>
		private void handleError( object sender, EventArgs e ) {
			ExecuteWithBasicExceptionHandling(
				() => {
					try {
						Exception exception;
						try {
							// This code should happen first to prevent errors from going to the Windows event log.
							exception = Server.GetLastError();
							Server.ClearError();

							rollbackDatabaseTransactionsAndClearResponse();
						}
						finally {
							CompleteRequest();
						}

						var errorIsWcf404 = exception.InnerException is System.ServiceModel.EndpointNotFoundException;

						// We can remove this as soon as requesting a URL with a vertical pipe doesn't blow up our web applications.
						var errorIsBogusPathException = exception is ArgumentException argException && argException.Message == "Illegal characters in path.";

						var baseUrlRequest = new Lazy<bool>(
							() => string.Equals(
								RequestState.Url,
								EwfConfigurationStatics.AppConfiguration.DefaultBaseUrl.GetUrlString( EwfConfigurationStatics.AppSupportsSecureConnections ),
								StringComparison.Ordinal ) );
						if( exception is ResourceNotAvailableException || errorIsWcf404 || errorIsBogusPathException )
							transferRequest( 404, getErrorPage( new ResourceNotAvailable( !baseUrlRequest.Value ) ) );
						else if( exception is AccessDeniedException accessDeniedException ) {
							if( accessDeniedException.CausedByIntermediateUser )
								transferRequest( 403, new NonLiveLogIn( RequestState.Url ) );
							else if( UserManagementStatics.UserManagementEnabled && !ConfigurationStatics.IsLiveInstallation && RequestState.UserAccessible &&
							         !RequestState.ImpersonatorExists )
								transferRequest( 403, new Impersonate( RequestState.Url ) );
							else if( UserManagementStatics.UserManagementEnabled && FormsAuthStatics.FormsAuthEnabled )
								if( accessDeniedException.LogInPage != null )
									// We pass false here to avoid complicating things with ThreadAbortExceptions.
									Response.Redirect( accessDeniedException.LogInPage.GetUrl(), false );
								else
									transferRequest( 403, new LogIn( RequestState.Url ) );
							else
								transferRequest( 403, getErrorPage( new AccessDenied( !baseUrlRequest.Value ) ) );
						}
						else if( exception is PageDisabledException pageDisabledException )
							transferRequest( null, new ResourceDisabled( pageDisabledException.Message ) );
						else {
							RequestState.AddError( "", exception );
							transferRequestToUnhandledExceptionPage();
						}
					}
					catch {
						Response.ClearHeaders();
						Response.ClearContent();
						throw;
					}
				},
				true,
				true );
		}

		private void transferRequest( int? statusCode, ResourceBase resource ) {
			if( statusCode.HasValue ) {
				HttpContext.Current.Response.StatusCode = statusCode.Value;
				HttpContext.Current.Response.TrySkipIisCustomErrors = true;
			}

			try {
				resource.HandleRequest( HttpContext.Current, true );
			}
			catch( Exception exception ) {
				rollbackDatabaseTransactionsAndClearResponse();
				RequestState.AddError( "An exception occurred during a request for a handled error page or a log-in page:", exception );
				transferRequestToUnhandledExceptionPage();
			}
		}

		private void transferRequestToUnhandledExceptionPage() {
			HttpContext.Current.Response.StatusCode = 500;
			HttpContext.Current.Response.TrySkipIisCustomErrors = true;

			try {
				getErrorPage( new UnhandledException() ).HandleRequest( HttpContext.Current, true );
			}
			catch( Exception exception ) {
				rollbackDatabaseTransactionsAndClearResponse();
				RequestState.AddError( "An exception occurred during a request for the unhandled exception page:", exception );
				set500StatusCode( "Unhandled Exception Page Error" );
			}
		}

		private PageBase getErrorPage( PageBase ewfErrorPage ) {
			return errorPage ?? ewfErrorPage;
		}

		/// <summary>
		/// Gets the page that users will be redirected to when errors occur in the application.
		/// </summary>
		protected virtual PageBase errorPage => null;

		private void rollbackDatabaseTransactionsAndClearResponse() {
			RequestState.RollbackDatabaseTransactions();
			DataAccessState.Current.ResetCache();

			RequestState.EwfPageRequestState = new EwfPageRequestState( AppRequestState.RequestTime, null, null );

			Response.ClearHeaders();
			Response.ClearContent();
		}

		private void set500StatusCode( string description ) {
			Response.StatusCode = 500;
			Response.StatusDescription = description + " in EWF Application";
			Response.TrySkipIisCustomErrors = false;
		}
	}
}