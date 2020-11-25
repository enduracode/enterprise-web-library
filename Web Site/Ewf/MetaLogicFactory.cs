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
			infos.Add(
				new ExternalResource(
					"//fonts.googleapis.com/css2?family=Libre+Franklin:wght@500;600;700&family=Open+Sans:ital,wght@0,400;0,600;0,700;1,400&display=fallback" ) );
			infos.Add( new ExternalResource( "//maxcdn.bootstrapcdn.com/font-awesome/4.5.0/css/font-awesome.min.css" ) );
			infos.Add( new VersionedStaticFiles.ThirdParty.JQueryUi.Jquery_ui_1114custom_v2.Jquery_uiminCss.Info() );
			infos.Add( new ThirdParty.SelectCssCss.Info() );
			infos.Add( new VersionedStaticFiles.ThirdParty.Chosen.Chosen_v187.ChosenminCss.Info() );
			infos.Add( new ThirdParty.TimePicker.StylesCss.Info() );
			infos.Add( new ExternalResource( "//cdn.jsdelivr.net/qtip2/2.2.1/jquery.qtip.min.css" ) );
			infos.Add( new ExternalResource( "//cdnjs.cloudflare.com/ajax/libs/dialog-polyfill/0.4.9/dialog-polyfill.min.css" ) );
			infos.Add( new Styles.BasicCss.Info() );
			return infos;
		}

		IEnumerable<ResourceInfo> AppMetaLogicFactory.CreateEwfUiCssInfos() {
			return new ResourceInfo[]
				{
					new Styles.EwfUi.ColorsCss.Info(), new Styles.EwfUi.FontsCss.Info(), new Styles.EwfUi.LayoutCss.Info(), new Styles.EwfUi.TransitionsCss.Info()
				};
		}

		ResourceInfo AppMetaLogicFactory.CreateModernizrJavaScriptInfo() {
			return new ModernizrJs.Info();
		}

		IEnumerable<ResourceInfo> AppMetaLogicFactory.CreateJavaScriptInfos() {
			var infos = new List<ResourceInfo>();
			infos.Add( new ExternalResource( "//code.jquery.com/jquery-1.12.3.min.js" ) );
			infos.Add( new VersionedStaticFiles.ThirdParty.JQueryUi.Jquery_ui_1114custom_v2.Jquery_uiminJs.Info() );
			infos.Add( new VersionedStaticFiles.ThirdParty.Chosen.Chosen_v187.ChosenjqueryminJs.Info() );
			infos.Add( new ThirdParty.TimePicker.JavaScriptJs.Info() );
			infos.Add( new ExternalResource( "//cdn.jsdelivr.net/qtip2/2.2.1/jquery.qtip.min.js" ) );
			infos.Add( new ExternalResource( "//cdnjs.cloudflare.com/ajax/libs/dialog-polyfill/0.4.9/dialog-polyfill.min.js" ) );
			infos.Add( new ThirdParty.SpinJs.SpinminJs.Info() );
			infos.Add( new ExternalResource( "//cdn.ckeditor.com/4.5.8/full/ckeditor.js" ) );
			infos.Add( new ExternalResource( "https://cdnjs.cloudflare.com/ajax/libs/Chart.js/2.9.4/Chart.min.js" ) );
			infos.Add( new JavaScriptJs.Info() );
			return infos;
		}
	}
}