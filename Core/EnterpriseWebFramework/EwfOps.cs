using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.EnterpriseWebFramework.Core;
using EnterpriseWebLibrary.EnterpriseWebFramework.Ui;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;
using EnterpriseWebLibrary.ExternalFunctionality;
using EnterpriseWebLibrary.UserManagement;
using EnterpriseWebLibrary.WebSessionState;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using StackExchange.Profiling;
using StackExchange.Profiling.Internal;
using StackExchange.Profiling.Storage;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public static class EwfOps {
		/// <summary>
		/// Development Utility and private use only.
		/// </summary>
		public const int InitializationTimeoutSeconds = 120;

		private class MiniProfilerConfigureOptions: IConfigureOptions<MiniProfilerOptions> {
			private readonly IMemoryCache cache;

			public MiniProfilerConfigureOptions( IMemoryCache cache ) {
				this.cache = cache;
			}

			void IConfigureOptions<MiniProfilerOptions>.Configure( MiniProfilerOptions options ) {
				options.RouteBasePath = "/profiler";
				options.Storage = new MemoryCacheStorage( cache, TimeSpan.FromMinutes( 30 ) );
				options.ShouldProfile = _ => false;
			}
		}

		/// <summary>
		/// Call this from your Program.cs file. Besides this call, there should be no other code in the file.
		/// </summary>
		/// <param name="globalInitializer">The system's global initializer. Do not pass null.</param>
		/// <param name="appInitializer">The application initializer, which performs web-site specific initialization and cleanup. If you have one of these you
		/// should name the class AppInitializer.</param>
		public static int RunApplication( SystemInitializer globalInitializer, SystemInitializer appInitializer = null ) {
			// If the machine was recently started, delay initialization to give database services time to warm up. This avoids errors during data access.
			if( TimeSpan.FromMilliseconds( GetTickCount64() ) < new TimeSpan( 0, 3, 0 ) )
				Thread.Sleep( new TimeSpan( 0, 1, 0 ) );

			var initTimeDataAccessState = new ThreadLocal<DataAccessState>( () => new DataAccessState() );
			GlobalInitializationOps.InitStatics(
				globalInitializer,
				Path.GetFileName( Directory.GetCurrentDirectory() ),
				false,
				telemetryAppErrorContextWriter: writer => {
					// This check ensures that there is an actual request, which is not the case during application initialization.
					if( EwfApp.RequestState != null ) {
						writer.WriteLine();
						writer.WriteLine( "URL: " + AppRequestState.Instance.Url );

						if( EwfRequest.Current.AspNetRequest.HasFormContentType ) {
							writer.WriteLine();
							foreach( var i in EwfRequest.Current.AspNetRequest.Form )
								writer.WriteLine( "Form field " + i.Key + ": " + i.Value.Single() );
						}

						writer.WriteLine();
						foreach( var cookie in EwfRequest.Current.AspNetRequest.Cookies )
							writer.WriteLine( "Cookie " + cookie.Key + ": " + cookie.Value );

						writer.WriteLine();
						writer.WriteLine( "User agent: " + EwfRequest.Current.Headers.UserAgent );
						writer.WriteLine( "Referrer: " + EwfRequest.Current.Headers.Referer );

						User user = null;
						User impersonator = null;

						// exception-prone code
						try {
							user = AppTools.User;
							impersonator = AppRequestState.Instance.ImpersonatorExists ? AppRequestState.Instance.ImpersonatorUser : null;
						}
						catch {}

						if( user != null )
							writer.WriteLine(
								"User: {0}{1}".FormatWith( user.Email, impersonator != null ? " (impersonated by {0})".FormatWith( impersonator.Email ) : "" ) );
					}
				},
				mainDataAccessStateGetter: () => EwfApp.RequestState != null ? EwfApp.RequestState.DataAccessState : initTimeDataAccessState.Value );
			var frameworkInitialized = false;
			try {
				return GlobalInitializationOps.ExecuteAppWithStandardExceptionHandling(
					() => {
						try {
							EwfConfigurationStatics.Init();

							var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(
								new WebApplicationOptions
									{
										EnvironmentName = ConfigurationStatics.IsDevelopmentInstallation ? Environments.Development : Environments.Production,
										ContentRootPath = EwfConfigurationStatics.AppConfiguration.Path
									} );

							builder.WebHost.ConfigureKestrel( options => { options.AllowSynchronousIO = true; } );
							if( ConfigurationStatics.IsDevelopmentInstallation && EwfConfigurationStatics.AppConfiguration.UsesKestrel.Value )
								builder.Services.AddResponseCompression( options => { options.EnableForHttps = true; } );

							builder.Services.Configure<IISServerOptions>( options => { options.AllowSynchronousIO = true; } );

							builder.Services.AddHttpContextAccessor();
							builder.Services.AddDistributedMemoryCache();
							builder.Services.AddSession(
								options => {
									var defaultAttributes = EwfConfigurationStatics.AppConfiguration.DefaultCookieAttributes;
									options.Cookie.Name = "{0}Session".FormatWith( defaultAttributes.NamePrefix ?? "" );
									options.Cookie.Domain = defaultAttributes.Domain ?? "";
									options.Cookie.Path = "/{0}".FormatWith( defaultAttributes.Path ?? EwfConfigurationStatics.AppConfiguration.DefaultBaseUrl.Path );
									options.Cookie.SecurePolicy = CookieSecurePolicy.None;
									options.Cookie.IsEssential = true;

									options.IdleTimeout = AuthenticationStatics.SessionDuration.ToTimeSpan();
								} );
							builder.Services.AddDataProtection();

							// MiniProfiler
							builder.Services.AddMemoryCache();
							builder.Services.AddSingleton<IConfigureOptions<MiniProfilerOptions>, MiniProfilerConfigureOptions>();

							if( ExternalFunctionalityStatics.SamlFunctionalityEnabled )
								ExternalFunctionalityStatics.ExternalSamlProvider.RegisterDependencyInjectionServices( builder.Services );

							var app = builder.Build();

							using( var serviceScope = app.Services.CreateScope() ) {
								MiniProfiler.Configure( app.Services.GetRequiredService<IOptions<MiniProfilerOptions>>().Value );

								var providerGetter = new SystemProviderGetter(
									ConfigurationStatics.AppAssembly,
									ConfigurationStatics.AppAssembly.GetTypes()
										.Single( i => typeof( AppRequestDispatchingProvider ).IsAssignableFrom( i ) && !i.IsInterface )
										.Namespace,
									providerName =>
										@"{0} provider not found in application. To implement, create a class named {0} in ""Your Website\Providers"" that derives from App{0}Provider."
											.FormatWith( providerName ) );

								var contextAccessor = app.Services.GetRequiredService<IHttpContextAccessor>();
								if( ExternalFunctionalityStatics.SamlFunctionalityEnabled )
									ExternalFunctionalityStatics.ExternalSamlProvider.InitAppStatics(
										() => AspNetStatics.Services,
										providerGetter,
										() => AuthenticationStatics.SamlIdentityProviders.Select(
												identityProvider => {
													using( var client = new HttpClient() ) {
														client.Timeout = new TimeSpan( 0, 0, 10 );
														var metadata = Task.Run(
																async () => {
																	using( var response = await client.GetAsync( identityProvider.MetadataUrl, HttpCompletionOption.ResponseHeadersRead ) ) {
																		response.EnsureSuccessStatusCode();
																		var document = new XmlDocument();
																		using( var stream = await response.Content.ReadAsStreamAsync() )
																			using( var reader = XmlReader.Create( stream ) )
																				document.Load( reader );
																		return document.DocumentElement;
																	}
																} )
															.Result;
														return ( metadata, identityProvider.EntityId );
													}
												} )
											.Materialize() );

								AspNetStatics.Init( () => contextAccessor.HttpContext?.RequestServices ?? serviceScope.ServiceProvider );
								EwfRequest.Init( providerGetter.GetProvider<AppRequestBaseUrlProvider>( "RequestBaseUrl" ), () => contextAccessor.HttpContext.Request );
								EwfResponse.Init( () => contextAccessor.HttpContext.Response );
								UrlHandlingStatics.Init(
									() => RequestDispatchingStatics.AppProvider.GetBaseUrlPatterns(),
									( baseUrlString, appRelativeUrl ) =>
										AppRequestState.ExecuteWithUrlHandlerStateDisabled( () => UrlHandlingStatics.ResolveUrl( baseUrlString, appRelativeUrl )?.Last() ) );
								CookieStatics.Init(
									( name, value, options ) => {
										var cookies = contextAccessor.HttpContext.Response.Cookies;
										if( value != null )
											cookies.Append( name, value, options );
										else
											cookies.Delete( name, options );
									} );
								Translation.Init( () => "en-US" );
								CssPreprocessingStatics.Init( globalInitializer.GetType().Assembly, ConfigurationStatics.AppAssembly );
								ResourceBase.Init(
									( requestTransferred, resource ) => {
										if( requestTransferred ) {
											var urlHandlers = new List<BasicUrlHandler>();
											UrlHandler urlHandler = resource;
											do
												urlHandlers.Add( urlHandler );
											while( ( urlHandler = urlHandler.GetParent() ) != null );
											AppRequestState.Instance.SetUrlHandlers( urlHandlers );

											AppRequestState.Instance.SetNewUrlParameterValuesEffective( false );
											AppRequestState.Instance.SetResource( resource );
										}
										else
											AppRequestState.Instance.SetResource( resource );
									},
									() => AppRequestState.Instance.Resource );
								StaticFile.Init( providerGetter.GetProvider<AppStaticFileHandlingProvider>( "StaticFileHandling" ) );
								StandardLibrarySessionState.Init( () => contextAccessor.HttpContext );
								PageBase.Init(
									( () => BasePageStatics.AppProvider.GetPageViewDataModificationMethod(),
										() => BasePageStatics.AppProvider.JavaScriptDocumentReadyFunctionCall ),
									BasicPageContent.GetContent );
								HyperlinkBehaviorExtensionCreators.Init( ModalBox.GetBrowsingModalBoxOpenStatements );
								FileUpload.Init( () => ( (BasicPageContent)PageBase.Current.BasicContent ).FormUsesMultipartEncoding = true );
								ModalBox.Init( () => ( (BasicPageContent)PageBase.Current.BasicContent ).BrowsingModalBoxId );
								CreditCardCollector.Init( () => ( (BasicPageContent)PageBase.Current.BasicContent ).IncludesStripeCheckout = true );
								BasePageStatics.Init( providerGetter.GetProvider<AppStandardPageLogicProvider>( "StandardPageLogic" ) );
								BasicPageContent.Init(
									contentObjects => {
										var contentUsesUi = contentObjects.Any( i => i is UiPageContent );

										var cssInfos = new List<ResourceInfo>();
										cssInfos.Add(
											new ExternalResource(
												"//fonts.googleapis.com/css2?family=Libre+Franklin:wght@500;600;700&family=Open+Sans:ital,wght@0,400;0,600;0,700;1,400&display=fallback" ) );
										cssInfos.Add( new ExternalResource( "//maxcdn.bootstrapcdn.com/font-awesome/4.5.0/css/font-awesome.min.css" ) );
										cssInfos.Add( new StaticFiles.Versioned.Third_party.Jquery_ui.Jquery_ui_1114custom_v2.Jquery_uiminCss() );
										cssInfos.Add( new StaticFiles.Third_party.Select_cssCss() );
										cssInfos.Add( new StaticFiles.Versioned.Third_party.Chosen.Chosen_v187.ChosenminCss() );
										cssInfos.Add( new StaticFiles.Third_party.Time_picker.StylesCss() );
										cssInfos.Add( new ExternalResource( "//cdn.jsdelivr.net/qtip2/2.2.1/jquery.qtip.min.css" ) );
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

										var infos = new List<ResourceInfo>();
										infos.Add( new ExternalResource( "//code.jquery.com/jquery-1.12.3.min.js" ) );
										infos.Add( new StaticFiles.Versioned.Third_party.Jquery_ui.Jquery_ui_1114custom_v2.Jquery_uiminJs() );
										infos.Add( new StaticFiles.Versioned.Third_party.Chosen.Chosen_v187.ChosenjqueryminJs() );
										infos.Add( new StaticFiles.Third_party.Time_picker.CodeJs() );
										infos.Add( new ExternalResource( "//cdn.jsdelivr.net/qtip2/2.2.1/jquery.qtip.min.js" ) );
										infos.Add( new ExternalResource( "//cdnjs.cloudflare.com/ajax/libs/dialog-polyfill/0.4.9/dialog-polyfill.min.js" ) );
										infos.Add( new StaticFiles.Third_party.Spin_js.SpinminJs() );
										infos.Add( new ExternalResource( "//cdn.ckeditor.com/4.5.8/full/ckeditor.js" ) );
										infos.Add( new ExternalResource( "https://cdnjs.cloudflare.com/ajax/libs/Chart.js/2.9.4/Chart.min.js" ) );
										infos.Add( new ExternalResource( "https://instant.page/5.1.0" ) );
										if( includeStripeCheckout )
											infos.Add( new ExternalResource( "https://checkout.stripe.com/checkout.js" ) );
										infos.Add( new StaticFiles.CodeJs() );
										foreach( var i in infos.Select( getElement ) )
											markup.Append( i );
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

										return icons;
									},
									hideWarnings => {
										var url = AppRequestState.Instance.Url;
										if( AppRequestState.Instance.UserAccessible && AppRequestState.Instance.ImpersonatorExists )
											url = new UserManagement.Pages.Impersonate(
												url,
												optionalParameterSetter: ( specifier, parameters ) =>
													specifier.User = AppTools.User != null ? AppTools.User.Email : UserManagement.Pages.Impersonate.AnonymousUser ).GetUrl();
										return new NonLiveLogIn(
											url,
											optionalParameterSetter: ( specifier, parameters ) => {
												specifier.Password = ConfigurationStatics.SystemGeneralProvider.IntermediateLogInPassword;
												specifier.HideWarnings = hideWarnings;
											} ).GetUrl();
									},
									() => {
										if( !AppRequestState.Instance.UserAccessible || !AppRequestState.Instance.ImpersonatorExists ||
										    ( ConfigurationStatics.IsIntermediateInstallation && !AppRequestState.Instance.IntermediateUserExists ) )
											return null;
										return ( "User impersonation is in effect.",
											       new HyperlinkSetup( new UserManagement.Pages.Impersonate( AppRequestState.Instance.Url ), "Change user" )
												       .Append<ActionComponentSetup>(
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
									( user, code ) => new UserManagement.Pages.LogIn(
										"",
										optionalParameterSetter: ( specifier, _ ) => {
											specifier.User = user;
											specifier.Code = code;
										} ).GetUrl(),
									destinationUrl => new UserManagement.Pages.ChangePassword( destinationUrl ).GetUrl( disableAuthorizationCheck: true ) );
								Admin.EntitySetup.Init( () => RequestDispatchingStatics.AppProvider.GetFrameworkUrlParent() );
								RequestDispatchingStatics.Init(
									providerGetter.GetProvider<AppRequestDispatchingProvider>( "RequestDispatching" ),
									() => contextAccessor.HttpContext );

								appInitializer?.InitStatics();

								executeWithAutomaticDatabaseConnections( AuthenticationStatics.InitAppSpecificLogicDependencies );
								if( AuthenticationStatics.SamlIdentityProviders.Any() || ExternalFunctionalityStatics.SamlFunctionalityEnabled )
									executeWithAutomaticDatabaseConnections( ExternalFunctionalityStatics.ExternalSamlProvider.InitAppSpecificLogicDependencies );
							}

							initTimeDataAccessState = null;
							frameworkInitialized = true;

							if( ConfigurationStatics.IsDevelopmentInstallation && EwfConfigurationStatics.AppConfiguration.UsesKestrel.Value )
								app.UsePathBase( "/{0}".FormatWith( EwfConfigurationStatics.AppConfiguration.DefaultBaseUrl.Path ) );
							app.UseSession();
							if( ConfigurationStatics.IsDevelopmentInstallation && EwfConfigurationStatics.AppConfiguration.UsesKestrel.Value )
								app.UseResponseCompression();
							app.UseMiniProfiler(); // only used to handle MiniProfiler requests
							app.Use( RequestDispatchingStatics.ProcessRequest );
							app.UseRouting();
							app.Use( EwfApp.EnsureUrlResolved );

							app.Run();
						}
						finally {
							appInitializer?.CleanUpStatics();
						}
					} );
			}
			finally {
				GlobalInitializationOps.CleanUpStatics();

				// If initialization failed, ensure that we exceed the startupTimeLimit of the ASP.NET Core Module (ANCM). This will cause the module to recycle the IIS
				// application pool and therefore retry initialization.
				if( !frameworkInitialized && !ConfigurationStatics.IsDevelopmentInstallation )
					Thread.Sleep( TimeSpan.FromSeconds( InitializationTimeoutSeconds + 10 ) );
			}
		}

		[ DllImport( "kernel32" ) ]
		private static extern ulong GetTickCount64();

		private static void assertResourceIsIntermediateInstallationPublicResourceWhenNecessary( ResourceInfo resource ) {
			if( !PageBase.Current.IsIntermediateInstallationPublicResource )
				return;
			if( resource is ResourceBase appResource && !appResource.IsIntermediateInstallationPublicResource )
				throw new Exception(
					"You must specify resource {0} as an intermediate-installation public resource because it is used on an intermediate-installation public page."
						.FormatWith( resource.GetUrl( false, false ) ) );
		}

		private static void executeWithAutomaticDatabaseConnections( Action method ) {
			var connectionManager = new AutomaticDatabaseConnectionManager();
			try {
				connectionManager.DataAccessState.ExecuteWithThis( method );
				connectionManager.CommitTransactionsAndExecuteNonTransactionalModificationMethods( false );
			}
			catch {
				connectionManager.RollbackTransactions( false );
				throw;
			}
		}
	}
}