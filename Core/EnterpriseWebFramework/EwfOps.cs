#nullable disable
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Destructurama;
using EnterpriseWebLibrary.Caching;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.EnterpriseWebFramework.Core;
using EnterpriseWebLibrary.EnterpriseWebFramework.OpenIdProvider;
using EnterpriseWebLibrary.EnterpriseWebFramework.Ui;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;
using EnterpriseWebLibrary.EnterpriseWebFramework.WellKnownUrlHandling;
using EnterpriseWebLibrary.ExternalFunctionality;
using EnterpriseWebLibrary.UserManagement;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NodaTime;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using StackExchange.Profiling;
using StackExchange.Profiling.Internal;
using StackExchange.Profiling.Storage;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

[ PublicAPI ]
public static class EwfOps {
	/// <summary>
	/// Development Utility and private use only.
	/// </summary>
	public static readonly Duration InitializationTimeout = GlobalInitializationOps.MachineStartupDelay + Duration.FromSeconds( 150 );

	private class MiniProfilerConfigureOptions: IConfigureOptions<MiniProfilerOptions> {
		private class ProfilerProvider: DefaultProfilerProvider {
			public override MiniProfiler CurrentProfiler {
				get => EwfRequest.Current is not null ? RequestDispatchingStatics.RequestState.Profiler : null;
				protected set => RequestDispatchingStatics.RequestState.Profiler = value;
			}

			public override MiniProfiler Start( string profilerName, MiniProfilerBaseOptions options ) => new( profilerName ?? nameof(MiniProfiler), options );
		}

		private readonly IMemoryCache cache;

		public MiniProfilerConfigureOptions( IMemoryCache cache ) {
			this.cache = cache;
		}

		void IConfigureOptions<MiniProfilerOptions>.Configure( MiniProfilerOptions options ) {
			options.RouteBasePath = "/profiler";
			options.Storage = new MemoryCacheStorage( cache, TimeSpan.FromMinutes( 30 ) );
			options.ShouldProfile = _ => false;
			options.ProfilerProvider = new ProfilerProvider();

			if( ConfigurationStatics.IsLiveInstallation )
				options.ExcludeStackTraceSnippetFromCustomTimings = true;
			else
				options.StackMaxLength = 750;
		}
	}

	/// <summary>
	/// Call this from your Program.cs file. Besides this call, there should be no other code in the file.
	/// </summary>
	/// <param name="globalInitializer">The system's global initializer. Do not pass null.</param>
	/// <param name="dependencyInjectionServicesRegistrationMethod">A method that registers the dependency-injection services needed by the web application.
	/// </param>
	/// <param name="appInitializer">The application initializer, which performs web-site specific initialization and cleanup. If you have one of these you
	/// should name the class AppInitializer.</param>
	public static int RunApplication(
		SystemInitializer globalInitializer, Action<IServiceCollection> dependencyInjectionServicesRegistrationMethod = null,
		SystemInitializer appInitializer = null ) {
		// If the machine was recently started, delay initialization to give database services time to warm up. This avoids errors during data access.
		GlobalInitializationOps.DelayIfMachineWasRecentlyStarted();

		var initTimeDataAccessState = new Lazy<DataAccessState>( () => new DataAccessState() );
		GlobalInitializationOps.InitStatics(
			globalInitializer,
			Path.GetFileName( Directory.GetCurrentDirectory() ),
			false,
			telemetryAppErrorContextWriter: writer => {
				// This check ensures that there is an actual request, which is not the case during application initialization.
				if( EwfRequest.Current != null ) {
					writer.WriteLine();
					writer.WriteLine( "URL: " + EwfRequest.Current.Url );

					if( EwfRequest.Current.AspNetRequest.HasFormContentType ) {
						writer.WriteLine();
						foreach( var pair in EwfRequest.Current.AspNetRequest.Form )
						foreach( var value in pair.Value )
							writer.WriteLine( "Form field " + pair.Key + ": " + value );
					}

					writer.WriteLine();
					foreach( var cookie in EwfRequest.Current.AspNetRequest.Cookies )
						writer.WriteLine( "Cookie " + cookie.Key + ": " + cookie.Value );

					writer.WriteLine();
					writer.WriteLine( "User agent: " + EwfRequest.Current.Headers.UserAgent );
					writer.WriteLine( "Referrer: " + EwfRequest.Current.Headers.Referer );

					SystemUser user = null;
					SystemUser impersonator = null;

					// exception-prone code
					try {
						user = AppTools.User;
						impersonator = RequestDispatchingStatics.RequestState.ImpersonatorExists ? RequestDispatchingStatics.RequestState.ImpersonatorUser : null;
					}
					catch {}

					if( user != null )
						writer.WriteLine( "User: {0}{1}".FormatWith( user.Email, impersonator != null ? " (impersonated by {0})".FormatWith( impersonator.Email ) : "" ) );
				}
			},
			mainDataAccessStateGetter: () =>
				EwfRequest.Current is not null ? RequestDispatchingStatics.RequestState.DatabaseConnectionManager.DataAccessState : initTimeDataAccessState.Value,
			currentDatabaseConnectionManagerGetter: () => RequestDispatchingStatics.RequestState.DatabaseConnectionManager,
			currentUserGetter: () => EwfRequest.Current is not null ? RequestDispatchingStatics.RequestState.UserAndImpersonator.Item1 : null );
		var frameworkInitialized = false;
		try {
			return GlobalInitializationOps.ExecuteAppWithStandardExceptionHandling(
				() => {
					try {
						EwfConfigurationStatics.Init();

						var diagnosticLogLevelSwitch = new LoggingLevelSwitch( initialMinimumLevel: LogEventLevel.Information );
						var loggerConfiguration = new LoggerConfiguration().Destructure.JsonNetTypes()
							.MinimumLevel.ControlledBy( diagnosticLogLevelSwitch )
							.MinimumLevel.Override( "Microsoft.AspNetCore", LogEventLevel.Warning );
						loggerConfiguration = ConfigurationStatics.IsDevelopmentInstallation
							                      ? loggerConfiguration.WriteTo.Console()
							                      : loggerConfiguration.WriteTo.Async(
								                      c => c.File(
									                      EwfConfigurationStatics.AppConfiguration.DiagnosticLogFilePath,
									                      rollingInterval: RollingInterval.Infinite,
									                      rollOnFileSizeLimit: false,
									                      encoding: Encoding.UTF8 ) );
						Log.Logger = loggerConfiguration.CreateLogger();

						var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(
							new WebApplicationOptions
								{
									EnvironmentName = ConfigurationStatics.IsDevelopmentInstallation ? Environments.Development : Environments.Production,
									ContentRootPath = EwfConfigurationStatics.AppConfiguration.Path
								} );

						builder.WebHost.ConfigureKestrel(
							options => {
								options.Limits.MaxRequestBodySize = null;
								options.AllowSynchronousIO = true;
								options.AddServerHeader = false;
							} );
						if( ConfigurationStatics.IsDevelopmentInstallation && EwfConfigurationStatics.AppConfiguration.UsesKestrel.Value )
							builder.Services.AddResponseCompression( options => { options.EnableForHttps = true; } );

						builder.Services.Configure<IISServerOptions>( options => { options.AllowSynchronousIO = true; } );

						builder.Services.Configure<FormOptions>( options => { options.ValueCountLimit = 10000; } );

						builder.Services.AddDataProtection();
						builder.Services.AddMvcCore();

						builder.Host.UseSerilog();

						builder.Services.AddSingleton<IConfigureOptions<MiniProfilerOptions>, MiniProfilerConfigureOptions>();

						if( ExternalFunctionalityStatics.OpenIdConnectFunctionalityEnabled )
							ExternalFunctionalityStatics.ExternalOpenIdConnectProvider.RegisterDependencyInjectionServices( builder.Services );
						if( ExternalFunctionalityStatics.SamlFunctionalityEnabled )
							ExternalFunctionalityStatics.ExternalSamlProvider.RegisterDependencyInjectionServices( builder.Services );

						dependencyInjectionServicesRegistrationMethod?.Invoke( builder.Services );

						// Register these last so they cannot be overridden.
						builder.Services.AddSingleton( AppMemoryCache.UnderlyingCache );
						builder.Services.AddSingleton<IHttpContextAccessor, EwfHttpContextAccessor>();

						var app = builder.Build();

						var aspNetScriptKey = "{0}AspNetScript".FormatWith( EwlStatics.EwlInitialism.ToLowerInvariant() );
						using( var serviceScope = app.Services.CreateScope() ) {
							MiniProfiler.Configure( app.Services.GetRequiredService<IOptions<MiniProfilerOptions>>().Value );

							var providerGetter = getProviderGetter( ConfigurationStatics.AppAssembly );

							if( ExternalFunctionalityStatics.OpenIdConnectFunctionalityEnabled )
								ExternalFunctionalityStatics.ExternalOpenIdConnectProvider.InitAppStatics(
									() => AspNetStatics.Services,
									EwfConfigurationStatics.AppConfiguration.DefaultBaseUrl.GetUrlString( true ),
									OpenIdProviderStatics.GetCertificate,
									OpenIdProviderStatics.CertificatePassword,
									() => OpenIdProviderStatics.AppProvider.GetClients() );
							if( ExternalFunctionalityStatics.SamlFunctionalityEnabled )
								ExternalFunctionalityStatics.ExternalSamlProvider.InitAppStatics(
									() => AspNetStatics.Services,
									providerGetter,
									() => AuthenticationStatics.SamlIdentityProviders.Select(
											identityProvider => {
												using var client = new HttpClient();
												client.Timeout = new TimeSpan( 0, 0, 10 );
												var metadata = Task.Run(
														async () => {
															using var response = await client.GetAsync( identityProvider.MetadataUrl, HttpCompletionOption.ResponseHeadersRead );
															response.EnsureSuccessStatusCode();
															var document = new XmlDocument();
															await using( var stream = await response.Content.ReadAsStreamAsync() )
																using( var reader = XmlReader.Create( stream ) )
																	document.Load( reader );
															return document.DocumentElement;
														} )
													.Result;
												return ( metadata, identityProvider.EntityId );
											} )
										.Materialize() );

							var contextAccessor = app.Services.GetRequiredService<IHttpContextAccessor>();
							AspNetStatics.Init( () => contextAccessor.HttpContext?.RequestServices ?? serviceScope.ServiceProvider );
							EwfRequest.Init(
								providerGetter.GetProvider<AppRequestBaseUrlProvider>( "RequestBaseUrl" ),
								() => {
									var context = contextAccessor.HttpContext;
									return context is not null && context.Items.ContainsKey( RequestDispatchingStatics.RequestStateKey ) ? context.Request : null;
								},
								() => RequestDispatchingStatics.RequestState.BeginInstant,
								() => RequestDispatchingStatics.RequestState.Url,
								networkWaitTime => RequestDispatchingStatics.RequestState.AddNetworkWaitTime( networkWaitTime ) );
							EwfResponse.Init(
								() => contextAccessor.HttpContext,
								() => {
									var requestState = RequestDispatchingStatics.RequestState;
									if( requestState.RequestHandler is not null )
										return;

									var modMethods = requestState.GetUserRequestLogger()
										.ToCollection()
										.Append( AuthenticationStatics.GetUserCookieUpdater() )
										.Where( i => i is not null )
										.Materialize();
									if( modMethods.Any() )
										ResourceBase.ExecuteDataModificationMethod(
											() => {
												foreach( var i in modMethods )
													i();
											} );

									RequestState.ExecuteWithUrlHandlerStateDisabled(
										() => {
											try {
												AutomaticDatabaseConnectionManager.Current.CommitTransactionsAndExecuteNonTransactionalModificationMethods( true );
											}
											finally {
												DataAccessState.Current.ResetCache();
											}
										} );
								} );
							UrlHandlingStatics.Init(
								() => RequestDispatchingStatics.GetAppProvider().GetBaseUrlPatterns(),
								( baseUrlString, appRelativeUrl ) =>
									RequestState.ExecuteWithUrlHandlerStateDisabled( () => UrlHandlingStatics.ResolveUrl( baseUrlString, appRelativeUrl )?.Last() ) );
							CookieStatics.Init(
								() => RequestDispatchingStatics.RequestState.ResponseCookies,
								( name, value, options ) => {
									AutomaticDatabaseConnectionManager.AddNonTransactionalModificationMethod(
										() => {
											var cookies = contextAccessor.HttpContext.Response.Cookies;
											if( value != null )
												cookies.Append( name, value.Length == 0 ? CookieStatics.EmptyValue : value, options );
											else
												cookies.Delete( name, options );
										} );

									RequestDispatchingStatics.RequestState.ResponseCookies.Add( ( name, value, options ) );
								} );
							NonLiveInstallationStatics.Init();
							Translation.Init( () => "en-US" );
							CssPreprocessingStatics.Init( globalInitializer.GetType().Assembly, ConfigurationStatics.AppAssembly );
							EwfSafeRequestHandler.Init( ResourceBase.ExecuteDataModificationMethod );
							ResourceBase.Init(
								ResourceSerializationStatics.SerializeResource,
								ConfigurationStatics.GetSystemLibraryProvider<SystemResourceSerializationProvider>( "ResourceSerialization" ),
								getAppResourceSerializationProvider( providerGetter ),
								( requestTransferred, resource ) => {
									if( requestTransferred ) {
										var urlHandlers = new List<BasicUrlHandler>();
										UrlHandler urlHandler = resource;
										do
											urlHandlers.Add( urlHandler );
										while( ( urlHandler = urlHandler.GetParent() ) != null );
										RequestDispatchingStatics.RequestState.SetUrlHandlers( urlHandlers );

										RequestDispatchingStatics.RequestState.SetNewUrlParameterValuesEffective( false );
										RequestDispatchingStatics.RequestState.SetResource( resource );
									}
									else
										RequestDispatchingStatics.RequestState.SetResource( resource );
								},
								() => RequestDispatchingStatics.RequestState.Resource,
								RequestDispatchingStatics.RefreshRequestState );
							EntitySetupBase.Init( RequestState.ExecuteWithUrlHandlerStateDisabled );
							WellKnownResource.Init(
								() => RequestDispatchingStatics.GetAppProvider().GetFrameworkUrlParent(),
								() => OpenIdProviderStatics.GetWellKnownUrls().Concat( RequestDispatchingStatics.GetAppProvider().GetWellKnownUrls() ) );
							StaticFile.Init( providerGetter.GetProvider<AppStaticFileHandlingProvider>( "StaticFileHandling" ) );
							PageInfrastructure.RequestStateStatics.Init(
								url => RequestDispatchingStatics.RequestState.ClientSideNewUrl = url,
								() => RequestDispatchingStatics.RequestState.StatusMessages,
								messages => {
									var state = RequestDispatchingStatics.RequestState;
									state.StatusMessages = state.StatusMessages.Concat( messages ).Materialize();
								},
								() => RequestDispatchingStatics.RequestState.SecondaryResponseId,
								id => RequestDispatchingStatics.RequestState.SecondaryResponseId = id,
								() => RequestDispatchingStatics.RequestState.GetLastError(),
								() => RequestDispatchingStatics.RequestState.AllowSlowRequest(),
								( url, requestMethod, requestHandler ) => RequestContinuationDataStore.AddRequestState(
									url,
									requestMethod,
									RequestDispatchingStatics.RequestState,
									requestHandler ) );
							PageBase.Init(
								( () => BasePageStatics.AppProvider.GetPageViewDataModificationMethod(), () => BasePageStatics.AppProvider.JavaScriptPageInitFunctionCall ),
								BasicPageContent.GetContent );
							HyperlinkBehaviorExtensionCreators.Init( ModalBox.GetBrowsingModalBoxOpenStatements );
							FileUpload.Init( () => ( (BasicPageContent)PageBase.Current.BasicContent ).FormUsesMultipartEncoding = true );
							ModalBox.Init( () => ( (BasicPageContent)PageBase.Current.BasicContent ).BrowsingModalBoxId );
							CreditCardCollector.Init( () => ( (BasicPageContent)PageBase.Current.BasicContent ).IncludesStripeCheckout = true );
							BasePageStatics.Init( providerGetter.GetProvider<AppStandardPageLogicProvider>( "StandardPageLogic" ) );
							BasicPageContent.Init(
								() => RequestDispatchingStatics.RequestState.ClientSideNewUrl,
								contentObjects => {
									var contentUsesUi = contentObjects.Any( i => i is UiPageContent );

									var cssInfos = new List<ResourceInfo>();

									cssInfos.Add(
										new ExternalResource(
											"https://fonts.googleapis.com/css2?family=Libre+Franklin:wght@500;600;700&family=Open+Sans:ital,wght@0,400;0,600;0,700;1,400&family=Roboto+Mono&display=fallback" ) );
									var fontFamilies = BasePageStatics.AppProvider.GetGoogleFontsFamilySpecifications();
									if( fontFamilies is not null ) {
										var familyParameters = StringTools.ConcatenateWithDelimiter( "&", fontFamilies.Select( i => "family=" + i ) );
										if( familyParameters.Length > 0 )
											cssInfos.Add( new ExternalResource( $"https://fonts.googleapis.com/css2?{familyParameters}&display=fallback" ) );
									}

									cssInfos.Add( new ExternalResource( "//maxcdn.bootstrapcdn.com/font-awesome/4.7.0/css/font-awesome.min.css" ) );
									cssInfos.Add( new StaticFiles.Versioned.Third_party.Jquery_ui.Jquery_ui_1132custom_v2.Jquery_uiminCss() );
									cssInfos.Add( new ExternalResource( "https://cdn.datatables.net/2.1.2/css/dataTables.dataTables.min.css" ) );
									cssInfos.Add( new ExternalResource( "https://cdn.datatables.net/responsive/3.0.2/css/responsive.dataTables.min.css" ) );
									cssInfos.Add( new StaticFiles.Third_party.Select_cssCss() );
									cssInfos.Add( new StaticFiles.Versioned.Third_party.Chosen.Chosen_v187.ChosenminCss() );
									cssInfos.Add( new StaticFiles.Third_party.Qtip2.JqueryqtipminCss() );
									cssInfos.Add( new ExternalResource( "//cdnjs.cloudflare.com/ajax/libs/dialog-polyfill/0.4.9/dialog-polyfill.min.css" ) );
									cssInfos.Add( new StaticFiles.Styles.BasicCss() );
									if( contentUsesUi )
										cssInfos.AddRange(
											new ResourceInfo[]
												{
													new StaticFiles.Styles.Ui.ColorsCss(), new StaticFiles.Styles.Ui.FontsCss(), new StaticFiles.Styles.Ui.LayoutCss(),
													new StaticFiles.Styles.Ui.TransitionsCss(), new StaticFiles.Styles.Ui.NewUICss()
												} );
									foreach( var resource in BasePageStatics.AppProvider.GetStyleSheets() ) {
										assertResourceIsIntermediateInstallationPublicResourceWhenNecessary( resource );
										cssInfos.Add( resource );
									}
									if( contentUsesUi )
										foreach( var resource in EwfUiStatics.AppProvider.GetStyleSheets() ) {
											assertResourceIsIntermediateInstallationPublicResourceWhenNecessary( resource );
											cssInfos.Add( resource );
										}
									else
										foreach( var resource in BasePageStatics.AppProvider.GetCustomUiStyleSheets() ) {
											assertResourceIsIntermediateInstallationPublicResourceWhenNecessary( resource );
											cssInfos.Add( resource );
										}
									return cssInfos;
								},
								( markup, includeStripeCheckout ) => {
									string getElement( ResourceInfo resource ) => "<script src=\"{0}\" defer></script>".FormatWith( resource.GetUrl() );

									markup.Append( getElement( new ExternalResource( "https://cdn.jsdelivr.net/npm/luxon@3.3.0/build/global/luxon.min.js" ) ) );
									markup.Append( getElement( new ExternalResource( "//code.jquery.com/jquery-3.6.3.min.js" ) ) );
									markup.Append( getElement( new StaticFiles.Versioned.Third_party.Jquery_ui.Jquery_ui_1132custom_v2.Jquery_uiminJs() ) );
									markup.Append( getElement( new ExternalResource( "https://cdn.datatables.net/2.1.2/js/dataTables.min.js" ) ) );
									markup.Append( getElement( new ExternalResource( "https://cdn.datatables.net/responsive/3.0.2/js/dataTables.responsive.min.js" ) ) );
									markup.Append( getElement( new StaticFiles.Versioned.Third_party.Chosen.Chosen_v187.ChosenjqueryminJs() ) );
									markup.Append( "<script type=\"module\" src=\"https://cdn.jsdelivr.net/npm/@duetds/date-picker@1.4.0/dist/duet/duet.esm.js\"></script>" );
									markup.Append( "<script nomodule src=\"https://cdn.jsdelivr.net/npm/@duetds/date-picker@1.4.0/dist/duet/duet.js\"></script>" );
									markup.Append( getElement( new StaticFiles.Third_party.Qtip2.JqueryqtipminJs() ) );
									markup.Append( getElement( new ExternalResource( "//cdnjs.cloudflare.com/ajax/libs/dialog-polyfill/0.4.9/dialog-polyfill.min.js" ) ) );
									markup.Append( getElement( new StaticFiles.Third_party.Spin_js.SpinminJs() ) );
									markup.Append( getElement( new ExternalResource( "https://cdn.ckeditor.com/4.22.1/full/ckeditor.js" ) ) );
									markup.Append( getElement( new ExternalResource( "https://cdnjs.cloudflare.com/ajax/libs/Chart.js/2.9.4/Chart.min.js" ) ) );
									markup.Append( getElement( new StaticFiles.Instant_pageJs() ) );
									if( includeStripeCheckout )
										markup.Append( getElement( new ExternalResource( "https://checkout.stripe.com/checkout.js" ) ) );
									markup.Append( getElement( new StaticFiles.CodeJs() ) );
									if( MiniProfiler.Current != null ) {
										var profiler = MiniProfiler.Current;
										var ids = profiler.Options.ExpireAndGetUnviewed( profiler.User ) ?? new List<Guid>( 1 );
										ids.Add( profiler.Id );
										markup.Append(
											Render.Includes(
												profiler,
												path: contextAccessor.HttpContext.Request.PathBase + ( (MiniProfilerOptions)profiler.Options ).RouteBasePath + "/",
												isAuthorized: true,
												null,
												requestIDs: ids ) );
									}
									foreach( var resource in BasePageStatics.AppProvider.GetJavaScriptFiles() ) {
										assertResourceIsIntermediateInstallationPublicResourceWhenNecessary( resource );
										markup.Append( getElement( resource ) );
									}

									if( contextAccessor.HttpContext.Items.TryGetValue( aspNetScriptKey, out var aspNetScriptGetter ) )
										markup.Append( getElement( new ExternalResource( ( (Func<string>)aspNetScriptGetter )() ) ) );
								},
								() => {
									var icons = new List<( ResourceInfo, string, string )>();

									var faviconPng48X48 = BasePageStatics.AppProvider.FaviconPng48X48;
									if( faviconPng48X48 != null ) {
										assertResourceIsIntermediateInstallationPublicResourceWhenNecessary( faviconPng48X48 );
										icons.Add( ( faviconPng48X48, "icon", "48x48" ) );
									}

									var favicon = BasePageStatics.AppProvider.Favicon;
									if( favicon != null ) {
										assertResourceIsIntermediateInstallationPublicResourceWhenNecessary( favicon );
										icons.Add( ( favicon, "icon", "" ) );
									}

									if( !icons.Any() )
										// see https://stackoverflow.com/a/62258760/35349
										icons.Add( ( new ExternalResource( "data:," ), "icon", "" ) );
									return icons;
								},
								hideWarnings => {
									var url = EwfRequest.Current.Url;
									if( RequestDispatchingStatics.RequestState.UserAccessible && RequestDispatchingStatics.RequestState.ImpersonatorExists )
										url = new UserManagement.Pages.Impersonate(
											url,
											optionalParameterSetter: ( specifier, _ ) =>
												specifier.User = AppTools.User != null ? AppTools.User.Email : UserManagement.Pages.Impersonate.AnonymousUser ).GetUrl();
									return new NonLiveLogIn(
										url,
										optionalParameterSetter: ( specifier, _ ) => {
											specifier.Password = ConfigurationStatics.SystemGeneralProvider.IntermediateLogInPassword;
											specifier.HideWarnings = hideWarnings;
										} ).GetUrl();
								},
								() => {
									if( !RequestDispatchingStatics.RequestState.UserAccessible || !RequestDispatchingStatics.RequestState.ImpersonatorExists ||
									    ( ConfigurationStatics.IsIntermediateInstallation && !RequestDispatchingStatics.RequestState.IntermediateUserExists ) )
										return null;
									return ( "User impersonation is in effect.",
										       new HyperlinkSetup( new UserManagement.Pages.Impersonate( EwfRequest.Current.Url ), "Change user" ).Append<ActionComponentSetup>(
												       new ButtonSetup(
													       "End impersonation",
													       behavior: new PostBackBehavior(
														       postBack: PostBack.CreateFull(
															       id: "ewfEndImpersonation",
															       modificationMethod: UserImpersonationStatics.EndImpersonation,
															       actionGetter: () => new PostBackAction(
																       new ExternalResource(
																	       EwfConfigurationStatics.AppConfiguration.DefaultBaseUrl.GetUrlString(
																		       EwfConfigurationStatics.AppSupportsSecureConnections ) ) ) ) ) ) )
											       .Materialize() );
								} );
							EwfUiStatics.Init( providerGetter.GetProvider<AppEwfUiProvider>( "EwfUi" ) );
							AuthenticationStatics.Init(
								providerGetter.GetProvider<AppAuthenticationProvider>( "Authentication" ),
								app.Services.GetRequiredService<IDataProtectionProvider>(),
								returnUrl => UserManagementStatics.LocalIdentityProviderEnabled || AuthenticationStatics.SamlIdentityProviders.Count > 1
									             ? new UserManagement.Pages.LogIn( returnUrl )
									             : new UserManagement.SamlResources.LogIn( AuthenticationStatics.SamlIdentityProviders.Single().EntityId, returnUrl ),
								( user, code ) => new UserManagement.Pages.LogIn(
									"",
									optionalParameterSetter: ( specifier, _ ) => {
										specifier.User = user;
										specifier.Code = code;
									} ).GetUrl(),
								destinationUrl => new UserManagement.Pages.ChangePassword( destinationUrl ).GetUrl( disableAuthorizationCheck: true ) );
							OpenIdProviderStatics.Init( providerGetter.GetProvider<AppOpenIdProviderProvider>( "OpenIdProvider" ) );
							Admin.EntitySetup.Init( () => RequestDispatchingStatics.GetAppProvider().GetFrameworkUrlParent(), diagnosticLogLevelSwitch );
							RequestDispatchingStatics.Init( getAppRequestDispatchingProvider( providerGetter ), () => contextAccessor.HttpContext );

							appInitializer?.InitStatics();

							var executeWithAutomaticDatabaseConnections = GlobalInitializationOps.ExecuteWithAutomaticDatabaseConnections;
							executeWithAutomaticDatabaseConnections( AuthenticationStatics.InitAppSpecificLogicDependencies );
							executeWithAutomaticDatabaseConnections( OpenIdProviderStatics.InitAppSpecificLogicDependencies );
							if( OpenIdProviderStatics.OpenIdProviderEnabled )
								executeWithAutomaticDatabaseConnections( ExternalFunctionalityStatics.ExternalOpenIdConnectProvider.InitAppSpecificLogicDependencies );
							if( AuthenticationStatics.SamlIdentityProviders.Any() || ExternalFunctionalityStatics.SamlFunctionalityEnabled )
								executeWithAutomaticDatabaseConnections( ExternalFunctionalityStatics.ExternalSamlProvider.InitAppSpecificLogicDependencies );
						}

						initTimeDataAccessState = null;
						frameworkInitialized = true;

						// See https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-watch#response-compression. We’re both causing BasicPageContent to include the
						// script, and suppressing the warning when it is, by tricking BrowserRefreshMiddleware into thinking that its own ResponseStreamWrapper injected
						// the script.
						if( Environment.GetEnvironmentVariable( "__ASPNETCORE_BROWSER_TOOLS" ) is not null )
							app.Use(
								async ( context, next ) => {
									var bodyFeature = context.Features.Get<IHttpResponseBodyFeature>();
									if( bodyFeature is StreamResponseBodyFeature streamBodyFeature ) {
										var stream = streamBodyFeature.Stream;
										context.Items.Add(
											aspNetScriptKey,
											() => {
												stream.GetType().GetProperty( "ScriptInjectionPerformed" ).SetValue( stream, true );
												return "/_framework/aspnetcore-browser-refresh.js";
											} );
									}

									await next( context );
								} );

						if( ConfigurationStatics.IsDevelopmentInstallation && EwfConfigurationStatics.AppConfiguration.UsesKestrel.Value )
							app.UsePathBase( "/{0}".FormatWith( EwfConfigurationStatics.AppConfiguration.DefaultBaseUrl.Path ) );
						if( ConfigurationStatics.IsDevelopmentInstallation && EwfConfigurationStatics.AppConfiguration.UsesKestrel.Value )
							app.UseResponseCompression();
						app.UseMiniProfiler(); // only used to handle MiniProfiler requests, and placed before Serilog middleware to exclude these requests from logging
						app.UseSerilogRequestLogging( options => { options.IncludeQueryInRequestPath = true; } );
						RequestDispatchingStatics.GetAppProvider().AddCustomMiddleware( app );
						app.Use( RequestDispatchingStatics.ProcessRequest );
						app.UseRouting();
						RequestDispatchingStatics.GetAppProvider().ConfigurePostFrameworkPipeline( app );
						app.Use( ensureUrlResolved );

						app.Run();
					}
					finally {
						appInitializer?.CleanUpStatics();
						Log.CloseAndFlush();
					}
				} );
		}
		finally {
			GlobalInitializationOps.CleanUpStatics();

			// If initialization failed, ensure that we exceed the startupTimeLimit of the ASP.NET Core Module (ANCM). This will cause the module to recycle the IIS
			// application pool and therefore retry initialization.
			if( !frameworkInitialized && !ConfigurationStatics.IsDevelopmentInstallation )
				Thread.Sleep( InitializationTimeout.Plus( Duration.FromSeconds( 10 ) ).ToTimeSpan() );
		}
	}

	private static void assertResourceIsIntermediateInstallationPublicResourceWhenNecessary( ResourceInfo resource ) {
		if( !PageBase.Current.IsIntermediateInstallationPublicResource )
			return;
		if( resource is ResourceBase { IsIntermediateInstallationPublicResource: false } )
			throw new Exception(
				"You must specify resource {0} as an intermediate-installation public resource because it is used on an intermediate-installation public page."
					.FormatWith( resource.GetUrl( false, false ) ) );
	}

	private static async Task ensureUrlResolved( HttpContext context, RequestDelegate next ) {
		if( context.GetEndpoint() == null )
			throw new ResourceNotAvailableException( "Failed to resolve the URL.", null );
		await next( context );
	}

	/// <summary>
	/// Initializes the calling application’s URL-generation functionality within the context of another web or non-web application in the system, enabling GetUrl
	/// methods to succeed. We recommend calling this from a static method in AppInitializer, which in turn should be called by the other application’s
	/// initializer.
	/// </summary>
	/// <param name="applicationName">The constant from WebApplicationNames that corresponds to the calling application.</param>
	public static void InitUrlGeneration( string applicationName ) {
		var appAssembly = Assembly.GetCallingAssembly();
		var providerGetter = getProviderGetter( appAssembly );

		UrlHandlingStatics.AddApplication(
			appAssembly,
			ConfigurationStatics.InstallationConfiguration.WebApplications.Single( i => string.Equals( i.Name, applicationName, StringComparison.Ordinal ) ),
			() => RequestDispatchingStatics.GetAppProvider( applicationName: applicationName ).GetBaseUrlPatterns(),
			( baseUrlString, appRelativeUrl ) => RequestState.ExecuteWithUrlHandlerStateDisabled(
				() => UrlHandlingStatics.ResolveUrl( baseUrlString, appRelativeUrl, appAssembly: appAssembly )?.Last() ) );
		ResourceBase.AddApplication( getAppResourceSerializationProvider( providerGetter ) );
		RequestDispatchingStatics.AddApplication( applicationName, getAppRequestDispatchingProvider( providerGetter ) );
	}

	private static SystemProviderGetter getProviderGetter( Assembly appAssembly ) =>
		new(
			appAssembly,
			appAssembly.GetTypes().Single( i => typeof( AppRequestDispatchingProvider ).IsAssignableFrom( i ) && !i.IsInterface ).Namespace,
			providerName =>
				@"{0} provider not found in application. To implement, create a class named {0} in ""Your Website\Providers"" that derives from App{0}Provider."
					.FormatWith( providerName ) );

	private static SystemProviderReference<AppResourceSerializationProvider> getAppResourceSerializationProvider( SystemProviderGetter providerGetter ) =>
		providerGetter.GetProvider<AppResourceSerializationProvider>( "ResourceSerialization" );

	private static SystemProviderReference<AppRequestDispatchingProvider> getAppRequestDispatchingProvider( SystemProviderGetter providerGetter ) =>
		providerGetter.GetProvider<AppRequestDispatchingProvider>( "RequestDispatching" );
}