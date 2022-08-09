using System.Web;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.EnterpriseWebFramework.ErrorPages;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;
using EnterpriseWebLibrary.UserManagement;
using Microsoft.AspNetCore.Http;
using StackExchange.Profiling;
using Tewl.Tools;
using static Humanizer.StringExtensions;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal static class EwfApp {
		private static Func<HttpContext> currentContextGetter;

		internal static void Init( Func<HttpContext> currentContextGetter ) {
			EwfApp.currentContextGetter = currentContextGetter;
		}

		internal static void ExecuteWithBasicExceptionHandling( Action handler, bool addErrorToRequestState, bool write500Response ) {
			try {
				handler();
			}
			catch( Exception e ) {
				EwlStatics.CallEveryMethod(
					delegate {
						const string prefix = "An exception occurred that could not be handled by the main exception handler:";
						if( addErrorToRequestState )
							RequestState.AddError( prefix, e );
						else
							TelemetryStatics.ReportError( prefix, e );
					},
					delegate {
						if( write500Response )
							EwfApp.write500Response( currentContextGetter(), "Exception" );
					} );
			}
		}

		/// <summary>
		/// Returns the request-state object for the current HTTP context.
		/// </summary>
		internal static AppRequestState RequestState {
			get => currentContextGetter != null /* i.e. IsWebApp */ ? (AppRequestState)currentContextGetter()?.Items[ EwlStatics.EwlInitialism ] : null;
			private set => currentContextGetter().Items.Add( EwlStatics.EwlInitialism, value );
		}

		internal static void HandleBeginRequest( HttpContext context ) {
			ExecuteWithBasicExceptionHandling(
				() => {
					// This used to be just HttpContext.Current.Request.Url, but that doesn't work with Azure due to the use of load balancing. An Azure load balancer
					// will bind to the ip/host/port through which all web requests should come in, and then the request is redirected to one of the server instances
					// running this web application. For example, a user will request mydomain.com. In Azure, there may be two instances running this web site, on
					// someIp:81 and someIp:82. For some reason, HttpContext.Current.Request.Url ends up using the host and port from one of these addresses instead of
					// using the host and port from the HTTP Host header, which is what the client is actually "viewing". Basically, HttpContext.Current.Request.Url
					// returns http://someIp:81?something=1 instead of http://mydomain.com?something=1. See
					// http://stackoverflow.com/questions/9560838/azure-load-balancer-causes-400-error-invalid-hostname-on-postback.

					var baseUrl = getRequestBaseUrl( context.Request );
					if( !baseUrl.Any() ) {
						EwfResponse.Create( "", new EwfResponseBodyCreator( () => "" ), statusCodeGetter: () => 400 ).WriteToAspNetResponse( context.Response );
						return;
					}

					var appRelativeUrl = context.Request.Path.Add( context.Request.QueryString );

					// If the base URL doesn't include a path and the app-relative URL is just a slash, don't include this trailing slash in the URL since it will not be
					// present in the canonical URLs that we construct and therefore it would cause problems with URL normalization.
					var url = !EwfRequest.AppBaseUrlProvider.GetRequestBasePath( context.Request ).Any() && appRelativeUrl.Length == "/".Length
						          ? baseUrl
						          : baseUrl + appRelativeUrl;


					RequestState = new AppRequestState( url, baseUrl );
				},
				false,
				true );
		}

		private static string getRequestBaseUrl( HttpRequest request ) {
			var provider = EwfRequest.AppBaseUrlProvider;
			var host = provider.GetRequestHost( request );
			return host.Any() ? BaseUrl.GetUrlString( provider.RequestIsSecure( request ), host, provider.GetRequestBasePath( request ) ) : "";
		}

		internal static void HandleAuthenticateRequest() {
			RequestState.IntermediateUserExists = NonLiveInstallationStatics.IntermediateAuthenticationCookieExists();
		}

		internal static void HandlePostAuthenticateRequest() {
			RequestState.EnableUser();
		}

		internal static Action<HttpContext> ResolveUrl( HttpContext context ) {
			using( MiniProfiler.Current.Step( "EWF - Resolve URL" ) )
				return resolveUrl( context );
		}

		private static Action<HttpContext> resolveUrl( HttpContext context ) {
			// Remove the leading slash if it exists. We are trying to normalize the difference between root applications and subdirectory applications by not
			// distinguishing between app-relative URLs of "" and "/". In root applications this distinction doesn’t exist. We’ve decided on a standard of never
			// allowing an app-relative URL of "/".
			var appRelativeUrl = context.Request.Path.Add( context.Request.QueryString );
			if( context.Request.Path.HasValue )
				appRelativeUrl = appRelativeUrl[ 1.. ];

			var handlers = RequestState.ExecuteWithUserDisabled(
				() => {
					try {
						return UrlHandlingStatics.ResolveUrl( RequestState.BaseUrl, appRelativeUrl );
					}
					catch( UnresolvableUrlException e ) {
						throw new ResourceNotAvailableException( "Failed to resolve the URL.", e );
					}
				} );
			if( handlers != null ) {
				// Before URL normalization, multiple copies of the same handler can exist in the list. When a new handler object is created and it matches more than
				// one handler in the list, we want parameters to be taken from the lowest-level segment. That’s why we reverse the handlers here.
				RequestState.SetUrlHandlers( handlers.Reverse().Materialize() );

				//if( handlers.Last() is PageBase || handlers.Last() is EntitySetupBase || handlers.Last() is PreBuiltResponse )
				//	HttpContext.Current.SetSessionStateBehavior( SessionStateBehavior.Required );
				return handlers.Last().HandleRequest;
			}

			// ACME challenge response; see https://tools.ietf.org/html/rfc8555#section-8.3
			var absoluteUrl = new Uri( RequestState.Url );
			if( absoluteUrl.Scheme == "http" && absoluteUrl.Port == 80 && absoluteUrl.AbsolutePath.StartsWith( "/.well-known/acme-challenge/" ) ) {
				var systemManager = ConfigurationStatics.MachineConfiguration?.SystemManager;
				if( systemManager != null )
					return c => ResourceBase.WriteRedirectResponse(
						c,
						systemManager.HttpBaseUrl.Replace( "https://", "http://" ) +
						"/Pages/Public/AcmeChallengeResponse.aspx;token={0}".FormatWith( HttpUtility.UrlEncode( absoluteUrl.Segments.Last() ) ),
						false );
			}

			return null;
		}

		internal static async Task EnsureUrlResolved( HttpContext context, RequestDelegate next ) {
			if( context.GetEndpoint() == null )
				throw new ResourceNotAvailableException( "Failed to resolve the URL.", null );
			await next( context );
		}

		internal static void HandleEndRequest() {
			if( RequestState == null )
				return;

			// Do not set a status code since we may have already set one or set a redirect page.
			ExecuteWithBasicExceptionHandling( () => RequestState.CleanUp(), false, false );
		}

		/// <summary>
		/// This method will not work properly unless RequestState is not null and the authenticated user is available.
		/// </summary>
		internal static void HandleError( HttpContext context, Exception exception ) {
			ExecuteWithBasicExceptionHandling(
				() => {
					try {
						rollbackDatabaseTransactionsAndClearResponse( context );

						// We can remove this as soon as requesting a URL with a vertical pipe doesn't blow up our web applications.
						var errorIsBogusPathException = exception is ArgumentException argException && argException.Message == "Illegal characters in path.";

						var baseUrlRequest = new Lazy<bool>(
							() => string.Equals(
								RequestState.Url,
								EwfConfigurationStatics.AppConfiguration.DefaultBaseUrl.GetUrlString( EwfConfigurationStatics.AppSupportsSecureConnections ),
								StringComparison.Ordinal ) );
						if( exception is ResourceNotAvailableException || errorIsBogusPathException )
							transferRequest( context, 404, getErrorPage( new ResourceNotAvailable( !baseUrlRequest.Value ) ) );
						else if( exception is AccessDeniedException accessDeniedException ) {
							if( accessDeniedException.CausedByIntermediateUser )
								transferRequest( context, 403, new NonLiveLogIn( RequestState.Url ) );
							else if( UserManagementStatics.UserManagementEnabled && !ConfigurationStatics.IsLiveInstallation && RequestState.UserAccessible &&
							         !RequestState.ImpersonatorExists )
								transferRequest( context, 403, new UserManagement.Pages.Impersonate( RequestState.Url ) );
							else if( accessDeniedException.LogInPage != null )
								transferRequest( context, 403, accessDeniedException.LogInPage );
							else if( UserManagementStatics.LocalIdentityProviderEnabled || AuthenticationStatics.SamlIdentityProviders.Count > 1 )
								transferRequest( context, 403, new UserManagement.Pages.LogIn( RequestState.Url ) );
							else if( AuthenticationStatics.SamlIdentityProviders.Any() )
								transferRequest(
									context,
									403,
									new UserManagement.SamlResources.LogIn( AuthenticationStatics.SamlIdentityProviders.Single().EntityId, RequestState.Url ) );
							else
								transferRequest( context, 403, getErrorPage( new AccessDenied( !baseUrlRequest.Value ) ) );
						}
						else if( exception is PageDisabledException pageDisabledException )
							transferRequest( context, null, new ResourceDisabled( pageDisabledException.Message ) );
						else {
							RequestState.AddError( "", exception );
							transferRequestToUnhandledExceptionPage( context );
						}
					}
					catch {
						context.Response.Clear();
						throw;
					}
				},
				true,
				true );
		}

		private static void transferRequest( HttpContext context, int? statusCode, ResourceBase resource ) {
			if( statusCode.HasValue )
				context.Response.StatusCode = statusCode.Value;

			try {
				resource.HandleRequest( context, true );
			}
			catch( Exception exception ) {
				rollbackDatabaseTransactionsAndClearResponse( context );
				RequestState.AddError( "An exception occurred during a request for a handled error page or a log-in page:", exception );
				transferRequestToUnhandledExceptionPage( context );
			}
		}

		private static void transferRequestToUnhandledExceptionPage( HttpContext context ) {
			context.Response.StatusCode = 500;

			try {
				getErrorPage( new UnhandledException() ).HandleRequest( context, true );
			}
			catch( Exception exception ) {
				rollbackDatabaseTransactionsAndClearResponse( context );
				RequestState.AddError( "An exception occurred during a request for the unhandled exception page:", exception );
				write500Response( context, "Unhandled Exception Page Error" );
			}
		}

		private static PageBase getErrorPage( PageBase ewfErrorPage ) => RequestDispatchingStatics.AppProvider.GetErrorPage() ?? ewfErrorPage;

		private static void rollbackDatabaseTransactionsAndClearResponse( HttpContext context ) {
			RequestState.RollbackDatabaseTransactions();
			DataAccessState.Current.ResetCache();

			RequestState.EwfPageRequestState = new EwfPageRequestState( AppRequestState.RequestTime, null, null );

			context.Response.Clear();
		}

		private static void write500Response( HttpContext context, string description ) {
			EwfResponse.Create(
					ContentTypes.PlainText,
					new EwfResponseBodyCreator( writer => writer.Write( "{0} in EWF Application".FormatWith( description ) ) ),
					statusCodeGetter: () => 500 )
				.WriteToAspNetResponse( context.Response, omitBody: string.Equals( context.Request.Method, "HEAD", StringComparison.Ordinal ) );
		}
	}
}