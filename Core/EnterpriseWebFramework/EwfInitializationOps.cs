﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Compilation;
using System.Web.Http;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.EnterpriseWebFramework.Ui;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;
using Humanizer;
using StackExchange.Profiling;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public static class EwfInitializationOps {
		private static bool ewlInitialized;
		private static SystemInitializer appInitializer;
		private static Timer initFailureUnloadTimer;

		/// <summary>
		/// Call this from Application_Start in your Global.asax.cs file. Besides this call, there should be no other code in the method.
		/// </summary>
		/// <param name="globalInitializer">The system's global initializer. Do not pass null.</param>
		/// <param name="appInitializer">The application initializer, which performs web-site specific initialization and cleanup. If you have one of these you
		/// should name the class AppInitializer.</param>
		public static void InitStatics( SystemInitializer globalInitializer, SystemInitializer appInitializer = null ) {
			// This is a hack to support data-access state in WCF services.
			var wcfDataAccessState = new ThreadLocal<DataAccessState>( () => new DataAccessState() );

			// Initialize system.
			var initTimeDataAccessState = new ThreadLocal<DataAccessState>( () => new DataAccessState() );
			try {
				GlobalInitializationOps.InitStatics(
					globalInitializer,
					Path.GetFileName( Path.GetDirectoryName( HttpRuntime.AppDomainAppPath ) ),
					false,
					mainDataAccessStateGetter: () => {
						return EwfApp.Instance != null ? EwfApp.Instance.RequestState != null ? EwfApp.Instance.RequestState.DataAccessState :
						                                 initTimeDataAccessState.Value :
						       System.ServiceModel.OperationContext.Current != null ? wcfDataAccessState.Value : null;
					} );
			}
			catch {
				// Suppress all exceptions since there is no way to report them.
				return;
			}
			ewlInitialized = true;

			// Initialize web application.
			if( !GlobalInitializationOps.SecondaryInitFailed )
				EwfApp.ExecuteWithBasicExceptionHandling(
					() => {
						EwfConfigurationStatics.Init();

						GlobalConfiguration.Configure( WebApiStatics.ConfigureWebApi );

						// Prevent MiniProfiler JSON exceptions caused by pages with hundreds of database queries.
						MiniProfiler.Settings.MaxJsonResponseSize = int.MaxValue;

						UrlHandlingStatics.Init(
							( baseUrlString, appRelativeUrl ) => {
								AppRequestState.Instance.UrlHandlerStateDisabled = true;
								try {
									return UrlHandlingStatics.ResolveUrl( baseUrlString, appRelativeUrl )?.Last();
								}
								finally {
									AppRequestState.Instance.UrlHandlerStateDisabled = false;
								}
							} );

						var globalType = BuildManager.GetGlobalAsaxType().BaseType;
						Func<ProviderType> getProviderGetter<ProviderType>( string providerName ) where ProviderType: class {
							var appAssembly = globalType.Assembly;
							var typeName = globalType.Namespace + ".Providers." + providerName;
							var provider = appAssembly.GetType( typeName ) != null ? appAssembly.CreateInstance( typeName ) as ProviderType : null;
							return () => provider ?? throw new ApplicationException(
								             providerName + " provider not found in application. To implement, create a class named " + providerName +
								             @" in ""Your Web Site\Providers"" that derives from App" + providerName + "Provider." );
						}

						CssPreprocessingStatics.Init( globalInitializer.GetType().Assembly, globalType.Assembly );
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
						PageBase.Init( BasicPageContent.GetContent );
						HyperlinkBehaviorExtensionCreators.Init( ModalBox.GetBrowsingModalBoxOpenStatements );
						FileUpload.Init( () => ( (BasicPageContent)PageBase.Current.BasicContent ).FormUsesMultipartEncoding = true );
						ModalBox.Init( () => ( (BasicPageContent)PageBase.Current.BasicContent ).BrowsingModalBoxId );
						CreditCardCollector.Init( () => ( (BasicPageContent)PageBase.Current.BasicContent ).IncludesStripeCheckout = true );
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
												new StaticFiles.Styles.Ui.TransitionsCss()
											} );
								foreach( var resource in EwfApp.Instance.GetStyleSheets() ) {
									assertResourceIsIntermediateInstallationPublicResourceWhenNecessary( resource );
									cssInfos.Add( resource );
								}
								if( contentUsesUi )
									foreach( var resource in EwfUiStatics.AppProvider.GetStyleSheets() ) {
										assertResourceIsIntermediateInstallationPublicResourceWhenNecessary( resource );
										cssInfos.Add( resource );
									}
								else
									foreach( var resource in EwfApp.Instance.GetCustomUiStyleSheets() ) {
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
								markup.Append( MiniProfiler.RenderIncludes().ToHtmlString() );
								foreach( var resource in EwfApp.Instance.GetJavaScriptFiles() ) {
									assertResourceIsIntermediateInstallationPublicResourceWhenNecessary( resource );
									markup.Append( getElement( resource ) );
								}
							},
							hideWarnings => {
								var url = AppRequestState.Instance.Url;
								if( AppRequestState.Instance.UserAccessible && AppRequestState.Instance.ImpersonatorExists )
									url = new UserManagement.Pages.Impersonate( url, optionalParameterSetter: ( specifier, parameters ) => specifier.User = AppTools.User.Email )
										.GetUrl();
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
									       new HyperlinkSetup( new UserManagement.Pages.Impersonate( AppRequestState.Instance.Url ), "Change user" ).Append<ActionComponentSetup>(
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
						EwfUiStatics.Init( getProviderGetter<AppEwfUiProvider>( "EwfUi" ) );
						Admin.EntitySetup.Init( () => RequestDispatchingStatics.AppProvider.GetFrameworkUrlParent() );
						RequestDispatchingStatics.Init( getProviderGetter<AppRequestDispatchingProvider>( "RequestDispatching" ) );

						EwfInitializationOps.appInitializer = appInitializer;
						appInitializer?.InitStatics();

						initTimeDataAccessState = null;
						EwfApp.FrameworkInitialized = true;
					},
					false,
					false );

			// If initialization failed, unload and restart the application after a reasonable delay.
			if( !EwfApp.FrameworkInitialized ) {
				const int unloadDelay = 60000; // milliseconds
				initFailureUnloadTimer = new Timer(
					state => EwfApp.ExecuteWithBasicExceptionHandling(
						() => {
							if( ConfigurationStatics.IsDevelopmentInstallation )
								return;
							HttpRuntime.UnloadAppDomain();

							// Restart the application by making a request. Idea from Rick Strahl:
							// http://weblog.west-wind.com/posts/2013/Oct/02/Use-IIS-Application-Initialization-for-keeping-ASPNET-Apps-alive.
							//
							// Disable server certificate validation so that this request gets through even for web sites that don't use a certificate that is trusted by
							// default. There is no security risk since we're not sending any sensitive information and we're not using the response.
							Tewl.Tools.NetTools.ExecuteHttpHeadRequest(
								IisConfigurationStatics.GetFirstBaseUrlForCurrentSite( false ),
								response => {},
								disableCertificateValidation: true );
						},
						false,
						false ),
					null,
					unloadDelay,
					Timeout.Infinite );
			}
		}

		private static void assertResourceIsIntermediateInstallationPublicResourceWhenNecessary( ResourceInfo resource ) {
			if( !PageBase.Current.IsIntermediateInstallationPublicResource )
				return;
			if( resource is ResourceBase appResource && !appResource.IsIntermediateInstallationPublicResource )
				throw new Exception(
					"You must specify resource {0} as an intermediate-installation public resource because it is used on an intermediate-installation public page."
						.FormatWith( resource.GetUrl( false, false ) ) );
		}

		/// <summary>
		/// Call this from Application_End in your Global.asax.cs file. Besides this call, there should be no other code in the method.
		/// </summary>
		public static void CleanUpStatics() {
			if( !ewlInitialized )
				return;

			if( !GlobalInitializationOps.SecondaryInitFailed )
				EwfApp.ExecuteWithBasicExceptionHandling( () => appInitializer?.CleanUpStatics(), false, false );

			GlobalInitializationOps.CleanUpStatics();

			if( !EwfApp.FrameworkInitialized ) {
				var waitHandle = new ManualResetEvent( false );
				initFailureUnloadTimer.Dispose( waitHandle );
				waitHandle.WaitOne();
			}
		}
	}
}