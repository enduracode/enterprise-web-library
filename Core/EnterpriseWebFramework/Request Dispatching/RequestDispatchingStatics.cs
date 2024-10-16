﻿using System.Threading;
using System.Threading.Tasks;
using System.Web;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.EnterpriseWebFramework.ErrorPages;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;
using EnterpriseWebLibrary.UserManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using StackExchange.Profiling;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

public static class RequestDispatchingStatics {
	private static readonly Dictionary<string, SystemProviderReference<AppRequestDispatchingProvider>> providersByAppName = new( StringComparer.Ordinal );
	internal const string RequestStateKey = EwlStatics.EwlInitialism;

	private static SystemProviderReference<AppRequestDispatchingProvider>? provider;
	private static Func<HttpContext>? currentContextGetter;
	private static FirstRequestCompletionTime? firstRequestCompletionTime;

	// This wrapper around Instant enables us to safely set the time using Interlocked.CompareExchange, which cannot support structs.
	private record FirstRequestCompletionTime( Instant Time );

	internal static void Init( SystemProviderReference<AppRequestDispatchingProvider> provider, Func<HttpContext> currentContextGetter ) {
		UnhandledException.Init( () => RequestState.GetLastError() );
		RequestState.Init( () => firstRequestCompletionTime?.Time );

		RequestDispatchingStatics.provider = provider;
		RequestDispatchingStatics.currentContextGetter = currentContextGetter;
	}

	internal static void AddApplication( string applicationName, SystemProviderReference<AppRequestDispatchingProvider> provider ) {
		providersByAppName.Add( applicationName, provider );
	}

	/// <summary>
	/// Framework use only.
	/// </summary>
	public static AppRequestDispatchingProvider GetAppProvider( string applicationName = "" ) =>
		applicationName.Length == 0 || string.Equals( applicationName, ConfigurationStatics.AppName, StringComparison.Ordinal )
			? provider!.GetProvider()!
			: providersByAppName[ applicationName ].GetProvider()!;

	internal static async Task ProcessRequest( HttpContext context, RequestDelegate next ) {
		var contextAccessor = (EwfHttpContextAccessor)context.RequestServices.GetRequiredService<IHttpContextAccessor>();
		contextAccessor.UseFrameworkContext = true;

		try {
			string? appRelativeUrl = null;
			await executeWithBasicExceptionHandling(
				context,
				async () => {
					// This used to be just HttpContext.Current.Request.Url, but that doesn’t work with Azure due to the use of load balancing. An Azure load balancer
					// will bind to the ip/host/port through which all web requests should come in, and then the request is redirected to one of the server instances
					// running this web application. For example, a user will request mydomain.com. In Azure, there may be two instances running this website, on
					// someIp:81 and someIp:82. For some reason, HttpContext.Current.Request.Url ends up using the host and port from one of these addresses instead of
					// using the host and port from the HTTP Host header, which is what the client is actually "viewing". Basically, HttpContext.Current.Request.Url
					// returns http://someIp:81?something=1 instead of http://mydomain.com?something=1. See
					// http://stackoverflow.com/questions/9560838/azure-load-balancer-causes-400-error-invalid-hostname-on-postback.

					var baseUrl = getRequestBaseUrl( context.Request );
					if( !baseUrl.Any() ) {
						EwfResponse.Create( "", new EwfResponseBodyCreator( () => "" ), statusCodeGetter: () => 400 )
							.WriteToAspNetResponse( context.Response, skipTransactionCommit: true );
						return;
					}

					var appRelativePath = UrlHandlingStatics.EncodePathForPredictableNormalization( context.Request.Path.ToUriComponent() );
					appRelativeUrl = appRelativePath + context.Request.QueryString.ToUriComponent();

					// If the base URL doesn’t include a path and the app-relative path is just a slash, don’t include this trailing slash in the URL since it will not be
					// present in the canonical URLs that we construct and therefore it would cause problems with URL normalization.
					var url = EwfRequest.AppBaseUrlProvider.GetRequestBasePath( context.Request ).Length == 0 && appRelativePath.Length == "/".Length
						          ? baseUrl + appRelativeUrl[ 1.. ]
						          : baseUrl + appRelativeUrl;


					context.Items.Add(
						RequestStateKey,
						await RequestContinuationDataStore.GetRequestState( url, baseUrl, context.Request.Method ) ?? new RequestState(
							context,
							url,
							baseUrl,
							GetAppProvider().GetSlowRequestThreshold() ) );
				},
				false );
			if( context.Response.StatusCode != 200 )
				return;

			try {
				Action<HttpContext>? requestHandler;
				if( RequestState.RequestHandler is not null ) {
					requestHandler = RequestState.RequestHandler;
					RequestState.RequestHandler = null;
					requestHandler( context );
					return;
				}

				RequestState.IntermediateUserExists = NonLiveInstallationStatics.IntermediateAuthenticationCookieExists();
				RequestState.EnableUser();

				var ipAddresses = GetAppProvider().GetWhitelistedIpAddressesForMaintenance();
				if( ipAddresses != null && !ipAddresses.Contains( context.Connection.RemoteIpAddress?.ToString()! ) ) {
					EwfResponse.Create( "", new EwfResponseBodyCreator( () => "" ), statusCodeGetter: () => 503 ).WriteToAspNetResponse( context.Response );
					return;
				}

				using( MiniProfiler.Current.Step( "EWF - Resolve URL" ) )
					requestHandler = resolveUrl( context, appRelativeUrl! );

				if( requestHandler is not null )
					requestHandler( context );
				else {
					contextAccessor.UseFrameworkContext = false;
					try {
						await next( context );
					}
					finally {
						contextAccessor.UseFrameworkContext = true;
					}
				}
			}
			catch( Exception exception ) {
				await handleError( context, exception );
			}
			finally {
				await executeWithBasicExceptionHandling(
					context,
					() => {
						if( RequestState.RequestHandler is not null )
							RequestState.ReleaseContinuationSemaphore();
						else
							RequestState.CleanUp( false );

						Interlocked.CompareExchange( ref firstRequestCompletionTime, new FirstRequestCompletionTime( SystemClock.Instance.GetCurrentInstant() ), null );

						return Task.CompletedTask;
					},
					false );
			}
		}
		finally {
			contextAccessor.UseFrameworkContext = false;
		}
	}

	private static string getRequestBaseUrl( HttpRequest request ) {
		var baseUrlProvider = EwfRequest.AppBaseUrlProvider;
		var host = baseUrlProvider.GetRequestHost( request );
		return host.Any() ? BaseUrl.GetUrlString( baseUrlProvider.RequestIsSecure( request ), host, baseUrlProvider.GetRequestBasePath( request ) ) : "";
	}

	private static Action<HttpContext>? resolveUrl( HttpContext context, string appRelativeUrl ) {
		// Remove the leading slash if it exists. We are trying to normalize the difference between root applications and subdirectory applications by not
		// distinguishing between app-relative URLs of "" and "/". In root applications this distinction doesn’t exist. We’ve decided on a standard of never
		// allowing an app-relative URL of "/".
		if( context.Request.Path.HasValue )
			appRelativeUrl = appRelativeUrl[ 1.. ];

		var handlers = RequestState.ExecuteWithUserDisabled(
			() => {
				try {
					return UrlHandlingStatics.ResolveUrl( RequestState.BaseUrl, appRelativeUrl );
				}
				catch( UnresolvableUrlException e ) {
					// An init method could take a long time to run, and then throw an exception, and there’d be no way for the resource to prevent slow-request errors.
					RequestState.AllowSlowRequest( allowUnlimitedTime: true );

					throw new ResourceNotAvailableException( "Failed to resolve the URL.", e );
				}
			} );
		if( handlers != null ) {
			// Before URL normalization, multiple copies of the same handler can exist in the list. When a new handler object is created and it matches more than
			// one handler in the list, we want parameters to be taken from the lowest-level segment. That’s why we reverse the handlers here.
			RequestState.SetUrlHandlers( handlers.Reverse().Materialize() );

			var handler = handlers.Last();
			allowSlowRequestIfNecessary( handler );

			return handler.HandleRequest;
		}

		// ACME challenge response; see https://www.rfc-editor.org/rfc/rfc8555#section-8.3
		var absoluteUrl = new Uri( EwfRequest.Current!.Url );
		if( string.Equals( absoluteUrl.Scheme, "http", StringComparison.Ordinal ) && absoluteUrl.Port == 80 &&
		    absoluteUrl.AbsolutePath.StartsWith( "/.well-known/acme-challenge/" ) ) {
			var systemManager = ConfigurationStatics.MachineConfiguration?.SystemManager;
			if( systemManager != null )
				return c => ResourceBase.WriteRedirectResponse(
					c,
					systemManager.HttpBaseUrl.Replace( "https://", "http://" ) +
					"acme-challenge-response;token={0}".FormatWith( HttpUtility.UrlEncode( absoluteUrl.Segments.Last() ) ),
					false );
		}

		return null;
	}

	private static async Task handleError( HttpContext context, Exception exception ) =>
		await executeWithBasicExceptionHandling(
			context,
			() => {
				try {
					if( !context.Response.HasStarted )
						context.Response.Clear();

					// We can remove this as soon as requesting a URL with a vertical pipe doesn't blow up our web applications.
					var errorIsBogusPathException = exception is ArgumentException argException && argException.Message == "Illegal characters in path.";

					var baseUrlRequest = new Lazy<bool>(
						() => string.Equals(
							EwfRequest.Current!.Url,
							EwfConfigurationStatics.AppConfiguration.DefaultBaseUrl.GetUrlString( EwfConfigurationStatics.AppSupportsSecureConnections ),
							StringComparison.Ordinal ) );
					if( exception is ResourceNotAvailableException || errorIsBogusPathException )
						transferRequest( context, 404, getErrorPage( new ResourceNotAvailable( !baseUrlRequest.Value ) ) );
					else if( exception is AccessDeniedException accessDeniedException ) {
						if( accessDeniedException.CausedByIntermediateUser )
							transferRequest( context, 403, new NonLiveLogIn( EwfRequest.Current!.Url ) );
						else if( UserManagementStatics.UserManagementEnabled && !ConfigurationStatics.IsLiveInstallation && RequestState.UserAccessible &&
						         !RequestState.ImpersonatorExists )
							transferRequest( context, 403, new UserManagement.Pages.Impersonate( EwfRequest.Current!.Url ) );
						else if( accessDeniedException.LogInPage != null )
							transferRequest( context, 403, accessDeniedException.LogInPage );
						else if( RequestState.UserAccessible && ( UserManagementStatics.LocalIdentityProviderEnabled ||
						                                          AuthenticationStatics.SamlIdentityProviders.Count > 1 ||
						                                          ( AuthenticationStatics.SamlIdentityProviders.Any() && SystemUser.Current is not null ) ) )
							transferRequest( context, 403, new UserManagement.Pages.LogIn( EwfRequest.Current!.Url ) );
						else if( RequestState.UserAccessible && AuthenticationStatics.SamlIdentityProviders.Any() )
							transferRequest(
								context,
								403,
								new UserManagement.SamlResources.LogIn( AuthenticationStatics.SamlIdentityProviders.Single().EntityId, EwfRequest.Current!.Url ) );
						else
							transferRequest( context, 403, getErrorPage( new AccessDenied( !baseUrlRequest.Value ) ) );
					}
					else if( exception is PageDisabledException pageDisabledException )
						transferRequest( context, null, new ResourceDisabled( pageDisabledException.Message ) );
					else if( exception is LogInException )
						transferRequest( context, 400, getErrorPage( new LogInSessionExpired() ) );
					else {
						AutomaticDatabaseConnectionManager.Current.RollbackTransactions( true );
						DataAccessState.Current.ResetCache();

						RequestState.AddError( "", exception );
						if( !context.Response.HasStarted )
							transferRequestToUnhandledExceptionPage( context );
					}
				}
				catch {
					AutomaticDatabaseConnectionManager.Current.RollbackTransactions( true );
					DataAccessState.Current.ResetCache();

					throw;
				}
				return Task.CompletedTask;
			},
			true );

	private static async Task executeWithBasicExceptionHandling( HttpContext context, Func<Task> handler, bool addErrorToRequestState ) {
		try {
			await handler();
		}
		catch( Exception e ) {
			const string prefix = "An exception occurred that could not be handled by the main exception handler:";
			ExceptionHandlingTools.CallEveryMethod(
				() => {
					if( addErrorToRequestState )
						RequestState.AddError( prefix, e );
					else
						TelemetryStatics.ReportError( prefix, e );
				},
				() => {
					if( context.Response.HasStarted )
						return;
					context.Response.Clear();
					write500Response( context, "Exception", ( prefix, e ) );
				} );
		}
	}

	private static void transferRequest( HttpContext context, int? statusCode, ResourceBase resource ) {
		if( statusCode.HasValue )
			context.Response.StatusCode = statusCode.Value;

		try {
			allowSlowRequestIfNecessary( resource );
			resource.HandleRequest( context, true );
		}
		catch( Exception exception ) {
			rollbackDatabaseTransactionsAndClearResponseIfPossible( context );
			RequestState.AddError( "An exception occurred during a request for a handled error page or a log-in page:", exception );
			if( !context.Response.HasStarted )
				transferRequestToUnhandledExceptionPage( context );
		}
	}

	private static void allowSlowRequestIfNecessary( BasicUrlHandler handler ) {
		if( handler is PageBase page ) {
			if( page.IsSlow )
				RequestState.AllowSlowRequest();
		}
		else
			RequestState.AllowSlowRequest( allowUnlimitedTime: true );
	}

	private static void transferRequestToUnhandledExceptionPage( HttpContext context ) {
		RequestState.StatusMessages = Array.Empty<( StatusMessageType, string )>();

		context.Response.StatusCode = 500;

		try {
			var page = getErrorPage( new UnhandledException() );
			if( page.IsSlow )
				RequestState.AllowSlowRequest();
			page.HandleRequest( context, true );
		}
		catch( Exception exception ) {
			rollbackDatabaseTransactionsAndClearResponseIfPossible( context );
			RequestState.AddError( "An exception occurred during a request for the unhandled exception page:", exception );
			if( !context.Response.HasStarted )
				write500Response( context, "Unhandled Exception Page Error", null );
		}
	}

	private static PageBase getErrorPage( PageBase ewfErrorPage ) => GetAppProvider().GetErrorPage() ?? ewfErrorPage;

	private static void rollbackDatabaseTransactionsAndClearResponseIfPossible( HttpContext context ) {
		AutomaticDatabaseConnectionManager.Current.RollbackTransactions( true );
		DataAccessState.Current.ResetCache();

		if( !context.Response.HasStarted )
			context.Response.Clear();
	}

	private static void write500Response( HttpContext context, string description, ( string prefix, Exception exception )? error ) {
		EwfResponse.Create(
				ContentTypes.PlainText,
				new EwfResponseBodyCreator(
					writer => {
						if( !ConfigurationStatics.IsDevelopmentInstallation ) {
							writer.Write( "{0} in EWF Application".FormatWith( description ) );
							return;
						}

						error ??= RequestState.GetLastError()!;

						if( error.Value.prefix.Length > 0 ) {
							writer.WriteLine( error.Value.prefix );
							writer.WriteLine();
						}
						writer.Write( error.Value.exception.ToString() );
					} ),
				statusCodeGetter: () => 500 )
			.WriteToAspNetResponse(
				context.Response,
				skipTransactionCommit: true,
				omitBody: string.Equals( context.Request.Method, "HEAD", StringComparison.Ordinal ) );
	}

	/// <summary>
	/// Returns a list of URL patterns for the framework, including one for well-known resources (i.e. the “.well-known” segment defined in RFC 8615).
	/// </summary>
	/// <param name="applicationName">The constant from WebApplicationNames that corresponds to this application.</param>
	/// <param name="frameworkUrlSegment">The URL segment that will be a base for the framework’s own pages and resources. Pass the empty string to use the
	/// default of “ewl”. Do not pass null.</param>
	/// <param name="appStaticFileUrlSegment">The URL segment that will be a base for the application’s static files. Pass the empty string to use the default
	/// of “static”. Do not pass null.</param>
	public static IReadOnlyCollection<UrlPattern> GetFrameworkUrlPatterns(
		string applicationName, string frameworkUrlSegment = "", string appStaticFileUrlSegment = "" ) {
		var patterns = new List<UrlPattern>();

		if( !frameworkUrlSegment.Any() )
			frameworkUrlSegment = EwlStatics.EwlInitialism.ToUrlSlug();
		patterns.Add( Admin.EntitySetup.UrlPatterns.Literal( frameworkUrlSegment ) );

		if( !appStaticFileUrlSegment.Any() )
			appStaticFileUrlSegment = "static";
		patterns.Add( GetAppProvider( applicationName: applicationName ).GetStaticFilesFolderUrlPattern( appStaticFileUrlSegment ) );

		patterns.Add(
			new UrlPattern(
				encoder => encoder is WellKnownUrlHandling.WellKnownResource.UrlEncoder wellKnownResource && wellKnownResource.GetSuffix().Length == 0
					           ? EncodingUrlSegment.Create( ".well-known" )
					           : null,
				url => string.Equals( url.Segment, ".well-known", StringComparison.OrdinalIgnoreCase )
					       ? new WellKnownUrlHandling.WellKnownResource.UrlDecoder( suffix: "" )
					       : null ) );

		return patterns;
	}

	/// <summary>
	/// Refreshes all state for the current request that could have become stale after data modifications, e.g. the authenticated user object.
	/// </summary>
	public static void RefreshRequestState() {
		RequestState.IntermediateUserExists = NonLiveInstallationStatics.IntermediateAuthenticationCookieExists();
		RequestState.ClearUserAndImpersonator();
	}

	/// <summary>
	/// Gets the request-state object for the current request. Throws an exception if called outside a request or from a non-web application. Framework use only.
	/// </summary>
	public static RequestState RequestState => (RequestState)currentContextGetter!().Items[ RequestStateKey ]!;
}