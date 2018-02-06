using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite {
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

		IEnumerable<ResourceInfo> AppMetaLogicFactory.CreateBasicCssInfos() {
			var infos = new List<ResourceInfo>();
			infos.Add( new ExternalResourceInfo( "//fonts.googleapis.com/css?family=Droid+Serif|Open+Sans:400,700,400italic" ) );
			infos.Add( new ExternalResourceInfo( "//maxcdn.bootstrapcdn.com/font-awesome/4.5.0/css/font-awesome.min.css" ) );
			infos.Add( new VersionedStaticFiles.ThirdParty.JQueryUi.Jquery_ui_1114custom_v2.Jquery_uiminCss.Info() );
			infos.Add( new VersionedStaticFiles.ThirdParty.Select2.Select2_343.Select2Css.Info() );
			infos.Add( new ThirdParty.TimePicker.StylesCss.Info() );
			infos.Add( new ExternalResourceInfo( "//cdn.jsdelivr.net/qtip2/2.2.1/jquery.qtip.min.css" ) );
			infos.Add( new ExternalResourceInfo( "//cdnjs.cloudflare.com/ajax/libs/dialog-polyfill/0.4.9/dialog-polyfill.min.css" ) );
			infos.Add( new VersionedStaticFiles.ThirdParty.JQueryModal_v1.JquerymodalCss.Info() );
			infos.Add( new Styles.BasicCss.Info() );
			return infos;
		}

		IEnumerable<ResourceInfo> AppMetaLogicFactory.CreateEwfUiCssInfos() {
			return new ResourceInfo[] { new Styles.EwfUi.ColorsCss.Info(), new Styles.EwfUi.FontsCss.Info(), new Styles.EwfUi.LayoutCss.Info() };
		}

		ResourceInfo AppMetaLogicFactory.CreateModernizrJavaScriptInfo() {
			return new ModernizrJs.Info();
		}

		IEnumerable<ResourceInfo> AppMetaLogicFactory.CreateJavaScriptInfos() {
			var infos = new List<ResourceInfo>();
			infos.Add( new ExternalResourceInfo( "//code.jquery.com/jquery-1.12.3.min.js" ) );
			infos.Add( new VersionedStaticFiles.ThirdParty.JQueryUi.Jquery_ui_1114custom_v2.Jquery_uiminJs.Info() );
			infos.Add( new VersionedStaticFiles.ThirdParty.Select2.Select2_343.Select2Js.Info() );
			infos.Add( new ThirdParty.TimePicker.JavaScriptJs.Info() );
			infos.Add( new ExternalResourceInfo( "//cdn.jsdelivr.net/qtip2/2.2.1/jquery.qtip.min.js" ) );
			infos.Add( new ExternalResourceInfo( "//cdnjs.cloudflare.com/ajax/libs/dialog-polyfill/0.4.9/dialog-polyfill.min.js" ) );
			infos.Add( new VersionedStaticFiles.ThirdParty.JQueryModal_v1.JquerymodalJs.Info() );
			infos.Add( new ThirdParty.SpinJs.SpinminJs.Info() );
			infos.Add( new ExternalResourceInfo( "//cdn.ckeditor.com/4.5.8/full/ckeditor.js" ) );
			infos.Add( new ExternalResourceInfo( "//cdnjs.cloudflare.com/ajax/libs/Chart.js/1.1.1/Chart.min.js" ) );
			infos.Add( new JavaScriptJs.Info() );
			return infos;
		}
	}
}