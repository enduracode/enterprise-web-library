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
using StackExchange.Profiling;

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

						var globalType = BuildManager.GetGlobalAsaxType().BaseType;
						if( !( globalType.Assembly.CreateInstance( "EnterpriseWebLibrary.EnterpriseWebFramework." + globalType.Namespace + ".MetaLogicFactory" ) is
							       AppMetaLogicFactory metaLogicFactory ) )
							throw new ApplicationException( "Meta logic factory not found." );
						EwfApp.Init( globalType, metaLogicFactory );

						CssPreprocessingStatics.Init( globalInitializer.GetType().Assembly, globalType.Assembly );
						EwfPage.Init( BasicPageContent.GetContent );
						HyperlinkBehaviorExtensionCreators.Init( ModalBox.GetBrowsingModalBoxOpenStatements );
						FileUpload.Init( () => ( (BasicPageContent)EwfPage.Instance.BasicContent ).FormUsesMultipartEncoding = true );
						ModalBox.Init( () => ( (BasicPageContent)EwfPage.Instance.BasicContent ).BrowsingModalBoxId );
						BasicPageContent.Init(
							contentObjects => {
								var contentUsesUi = contentObjects.Any( i => i is UiPageContent );

								var cssInfos = new List<ResourceInfo>();
								cssInfos.AddRange( EwfApp.MetaLogicFactory.CreateBasicCssInfos() );
								if( contentUsesUi )
									cssInfos.AddRange( EwfApp.MetaLogicFactory.CreateEwfUiCssInfos() );
								cssInfos.AddRange( EwfApp.Instance.GetStyleSheets() );
								if( contentUsesUi )
									cssInfos.AddRange( EwfUiStatics.AppProvider.GetStyleSheets() );
								return cssInfos;
							} );
						EwfUiStatics.Init( globalType );

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