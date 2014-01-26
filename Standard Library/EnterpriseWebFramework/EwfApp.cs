using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using RedStapler.StandardLibrary.Configuration.SystemGeneral;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.CssHandling;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Ui;
using RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement;
using StackExchange.Profiling;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
    /// <summary>
    /// The HttpApplication class for a web site using EWF. Provides access to the authenticated user, handles errors, and performs other useful functions.
    /// </summary>
    public abstract class EwfApp: HttpApplication {
        private static bool initialized;
        private static SystemGeneralConfigurationApplication webAppConfiguration;
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
            AppTools.Init( Path.GetFileName( Path.GetDirectoryName( HttpRuntime.AppDomainAppPath ) ),
                           false,
                           systemLogic,
                           mainDataAccessStateGetter: () => {
                               // We must use the Instance property here to prevent this logic from always returning the request state of the *first* EwfApp instance.
                               return Instance != null
                                          ? Instance.RequestState != null ? Instance.RequestState.DataAccessState : initTimeDataAccessState.Value
                                          : System.ServiceModel.OperationContext.Current != null ? wcfDataAccessState.Value : null;
                           } );
            if( AppTools.SecondaryInitFailed )
                return;

            // Initialize web application.
            executeWithBasicExceptionHandling( delegate {
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
                    CssHandlingStatics.Init( systemLogic.GetType().Assembly, GlobalType.Assembly );
                else
                    CssHandlingStatics.Init( GlobalType.Assembly );
                EwfUiStatics.Init( GlobalType );

                initializeWebApp();

                initTimeDataAccessState = null;
                initialized = true;
            },
                                               false,
                                               false );
        }

        /// <summary>
        /// Performs web-site specific initialization. Called at the end of ewfApplicationStart.
        /// </summary>
        protected abstract void initializeWebApp();

        /// <summary>
        /// Standard library use only.
        /// </summary>
        public static bool SupportsSecureConnections { get { return webAppConfiguration.SupportsSecureConnections || AppTools.IsIntermediateInstallation; } }

        private void handleBeginRequest( object sender, EventArgs e ) {
            if( !initialized ) {
                // We can't redirect to a normal page to communicate this information because since initialization failed, the request for that page will trigger
                // another BeginRequest event that puts us in an infinite loop. We can't rely on anything except an HTTP return code. Suppress exceptions; there is no
                // way to report them since even our basic exception handling won't work if the application isn't initialized.
                try {
                    set500StatusCode( "Initialization Failure" );
                }
                catch {}
                return;
            }

            executeWithBasicExceptionHandling( delegate {
                if( RequestState != null ) {
                    RequestState = null;
                    throw new ApplicationException( "AppRequestState was not properly cleaned up from a previous request." );
                }

                var hostHeader = HttpContext.Current.Request.Headers[ "Host" ];
                if( hostHeader == null ) {
                    setStatusCode( 400 );
                    return;
                }

                // This blocks until the entire request has been received from the client.
                // This won't compile unless it is assigned to something, which is why it is unused.
                var stream = Request.InputStream;

                RequestState = new AppRequestState( hostHeader );
            },
                                               false,
                                               true );
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
            // Remove the "~/" since it's part of every URL and is therefore useless when distinguishing between URLs.
            var url = Request.AppRelativeCurrentExecutionFilePath.Substring( NetTools.HomeUrl.Length );

            var ewfResolver = new ShortcutUrlResolver( "ewf",
                                                       ConnectionSecurity.SecureIfPossible,
                                                       () => {
                                                           var page = MetaLogicFactory.CreateBasicTestsPageInfo();
                                                           return page.UserCanAccessPageAndAllControls ? page : null;
                                                       } );

            foreach( var resolver in ewfResolver.ToSingleElementArray().Concat( GetShortcutUrlResolvers() ) ) {
                if( resolver.ShortcutUrl.ToLower() != url.ToLower() )
                    continue;

                // Redirect to the same shortcut URL to normalize the casing, fix the connection security, or both.
                var canonicalAbsoluteUrl =
                    NetTools.CombineUrls( GetBaseUrlWithSpecificSecurity( resolver.ConnectionSecurity.ShouldBeSecureGivenCurrentRequest( false ) ),
                                          resolver.ShortcutUrl );
                if( HttpRuntime.AppDomainAppVirtualPath == "/" && resolver.ShortcutUrl.Length == 0 )
                    canonicalAbsoluteUrl += "/";

                Action redirect = () => NetTools.Redirect( canonicalAbsoluteUrl );

                var canonicalUri = new Uri( canonicalAbsoluteUrl, UriKind.Absolute );
                var requestUri = new Uri( RequestState.Url, UriKind.Absolute );

                if( !IsSecureRequest && canonicalUri.Scheme == "https" )
                    redirect();

                if( !AppTools.InstallationConfiguration.DisableNonPreferredDomainChecking &&
                    canonicalAbsoluteUrl.Substring( ( canonicalUri.Scheme + "://" ).Length ) !=
                    RequestState.Url.Substring( ( requestUri.Scheme + "://" ).Length ) )
                    redirect();

                if( AppTools.IsIntermediateInstallation && !RequestState.IntermediateUserExists )
                    throw new AccessDeniedException( true, null );

                var page = resolver.Function();
                if( page == null )
                    throw new AccessDeniedException( false, resolver.LogInPageGetter != null ? resolver.LogInPageGetter() : null );
                if( page is ExternalPageInfo )
                    NetTools.Redirect( page.GetUrl() );
                HttpContext.Current.RewritePath( getTransferPath( page ), false );
                break;
            }
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
        protected internal virtual List<CssInfo> GetStyleSheets() {
            return new List<CssInfo>();
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
        /// Returns true if this application is accessed via https.
        /// A situation where this isn't as obvious as <see cref="HttpContext.Current.Request.IsSecureConnection"/> is when a reverse proxy is in place.
        /// </summary>
        public virtual bool IsSecureRequest { get { return Request.IsSecureConnection; } }

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

            executeWithBasicExceptionHandling( delegate {
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

            executeWithBasicExceptionHandling( delegate {
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
                    exception.InnerException is ResourceNotAvailableException || exception is ResourceNotAvailableException || onErrorProneAspNetHandler ||
                    errorIsWcf404 || errorIsBogusPathException ) {
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
                            if( RequestState.UserAccessible && AppTools.User == null && UserManagementStatics.UserManagementEnabled &&
                                UserManagementStatics.SystemProvider is FormsAuthCapableUserManagementProvider ) {
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
                // Suppress all exceptions since there is no way to report them.
                try {
                    StandardLibraryMethods.CallEveryMethod( delegate {
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

        private string getTransferPath( PageInfo page ) {
            var url = page.GetUrl( true, true, false );
            if( page.ShouldBeSecureGivenCurrentRequest != IsSecureRequest )
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

        internal static string GetBaseUrlWithSpecificSecurity( bool secure ) {
            var scheme = ( secure ? "https" : "http" ) + "://";
            if( !AppTools.InstallationConfiguration.BaseUrlOverride.IsWhitespace() )
                return scheme + AppTools.InstallationConfiguration.BaseUrlOverride;

            var appRequestState = AppRequestState.Instance;
            if( appRequestState == null ) {
                throw new ApplicationException(
                    "GetBaseUrlWithSpecificSecurity cannot be called outside the context of the web application unless BaseUrlOverride is specified." );
            }
            return NetTools.CombineUrls( scheme + appRequestState.Uri.Host + ( appRequestState.Uri.IsDefaultPort ? "" : ( ":" + appRequestState.Uri.Port ) ),
                                         HttpRuntime.AppDomainAppVirtualPath );
        }

        /// <summary>
        /// Call this from Application_End in your Global.asax.cs file. Besides this call, there should be no other code in the method.
        /// </summary>
        protected void ewfApplicationEnd() {}
    }
}