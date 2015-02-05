using System.Collections.Generic;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Ui;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite {
	internal class MetaLogicFactory: AppMetaLogicFactory {
		PageInfo AppMetaLogicFactory.CreateIntermediateLogInPageInfo( string returnUrl ) {
			return new IntermediateLogIn.Info( returnUrl );
		}

		PageInfo AppMetaLogicFactory.CreateLogInPageInfo( string returnUrl ) {
			return new UserManagement.LogIn.Info( returnUrl );
		}

		PageInfo AppMetaLogicFactory.CreateSelectUserPageInfo( string returnUrl ) {
			return new UserManagement.SelectUser.Info( returnUrl );
		}

		PageInfo AppMetaLogicFactory.CreatePreBuiltResponsePageInfo() {
			return new PreBuiltResponse.Info();
		}

		PageInfo AppMetaLogicFactory.CreateAccessDeniedErrorPageInfo( bool showHomeLink ) {
			return new ErrorPages.AccessDenied.Info( showHomeLink );
		}

		PageInfo AppMetaLogicFactory.CreatePageDisabledErrorPageInfo( string message ) {
			return new ErrorPages.PageDisabled.Info( message );
		}

		PageInfo AppMetaLogicFactory.CreatePageNotAvailableErrorPageInfo( bool showHomeLink ) {
			return new ErrorPages.PageNotAvailable.Info( showHomeLink );
		}

		PageInfo AppMetaLogicFactory.CreateUnhandledExceptionErrorPageInfo() {
			return new ErrorPages.UnhandledException.Info( "" );
		}

		PageInfo AppMetaLogicFactory.CreateBasicTestsPageInfo() {
			return Admin.BasicTests.GetInfo();
		}

		IEnumerable<ResourceInfo> AppMetaLogicFactory.CreateDisplayMediaCssInfos() {
			var infos = new List<ResourceInfo>();
			infos.Add( new ExternalResourceInfo( "//fonts.googleapis.com/css?family=Droid+Serif|Open+Sans:400,700,400italic" ) );
			infos.Add( new ExternalResourceInfo( "//netdna.bootstrapcdn.com/font-awesome/4.0.1/css/font-awesome.css" ) );
			infos.Add( new VersionedStaticFiles.ThirdParty.JqueryUi.Jquery_ui_1104Custom.Css.Custom_theme.Jquery_ui_1104CustomminCss.Info() );
			infos.Add( new VersionedStaticFiles.ThirdParty.Select2.Select2_343.Select2Css.Info() );
			infos.Add( new ThirdParty.TimePicker.StylesCss.Info() );
			infos.Add( new ExternalResourceInfo( "//cdn.jsdelivr.net/qtip2/2.2.0/jquery.qtip.min.css" ) );
			infos.Add( new Styles.BasicCss.Info() );
			if( EwfUiStatics.AppMasterPage != null ) {
				infos.Add( new Styles.EwfUi.ColorsCss.Info() );
				infos.Add( new Styles.EwfUi.FontsCss.Info() );
				infos.Add( new Styles.EwfUi.LayoutCss.Info() );
			}
			infos.Add( new Styles.ToDeleteOrMoveCss.Info() );

			if( AppRequestState.Instance.Browser.IsFirefox35OrBelow() )
				infos.Add( new Styles.FirefoxCss.Info() );
			if( AppRequestState.Instance.Browser.IsInternetExplorer() )
				infos.Add( new Styles.InternetExplorerCss.Info() );

			return infos;
		}

		IEnumerable<ResourceInfo> AppMetaLogicFactory.CreatePrintMediaCssInfos() {
			return new Styles.PrintCss.Info().ToSingleElementArray();
		}

		ResourceInfo AppMetaLogicFactory.CreateModernizrJavaScriptInfo() {
			return new ModernizrJs.Info();
		}

		IEnumerable<ResourceInfo> AppMetaLogicFactory.CreateJavaScriptInfos() {
			var infos = new List<ResourceInfo>();

			// See https://developers.google.com/speed/libraries/devguide. Keep in mind that we can't use a CDN for some of the other files since they are customized
			// versions.
			infos.Add( new ExternalResourceInfo( "//ajax.googleapis.com/ajax/libs/jquery/1.11.1/jquery.js" ) );

			infos.Add( new VersionedStaticFiles.ThirdParty.JqueryUi.Jquery_ui_1104Custom.Js.Jquery_ui_1104CustomminJs.Info() );
			infos.Add( new VersionedStaticFiles.ThirdParty.Select2.Select2_343.Select2Js.Info() );
			infos.Add( new ThirdParty.TimePicker.JavaScriptJs.Info() );
			infos.Add( new ExternalResourceInfo( "//cdn.jsdelivr.net/qtip2/2.2.0/jquery.qtip.min.js" ) );
			infos.Add( new ExternalResourceInfo( "//cdn.ckeditor.com/4.4.2/full/ckeditor.js" ) );
			infos.Add( new ThirdParty.ChartJs.ChartminJs.Info() );
			infos.Add( new JavaScriptJs.Info() );

			return infos;
		}
	}
}