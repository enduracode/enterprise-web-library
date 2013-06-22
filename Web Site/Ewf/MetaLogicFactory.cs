using System.Collections.Generic;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Ui;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite {
	internal class MetaLogicFactory: AppMetaLogicFactory {
		PageInfo AppMetaLogicFactory.GetIntermediateLogInPageInfo( string returnUrl ) {
			return new IntermediateLogIn.Info( returnUrl );
		}

		PageInfo AppMetaLogicFactory.GetLogInPageInfo( string returnUrl ) {
			return new UserManagement.LogIn.Info( returnUrl );
		}

		PageInfo AppMetaLogicFactory.CreateGetFilePageInfo() {
			return new GetFile.Info();
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

		IEnumerable<CssInfo> AppMetaLogicFactory.GetDisplayMediaCssInfos() {
			var infos = new List<CssInfo>();
			infos.Add( new JqueryUi.Css.Custom_theme.Jquery_ui_1101Custommin.Info() );
			infos.Add( new FontAwesome.Font_awesomemin.Info() );
			infos.Add( new ThirdParty.Select2.Select2_340.Select2.Info() );
			infos.Add( new ThirdParty.TimePicker.Styles.Info() );
			infos.Add( new ThirdParty.Qtip2.Jqueryqtipmin.Info() );
			infos.Add( new Styles.Basic.Info() );
			if( EwfUiStatics.AppMasterPage != null ) {
				infos.Add( new Styles.EwfUi.Colors.Info() );
				infos.Add( new Styles.EwfUi.Fonts.Info() );
				infos.Add( new Styles.EwfUi.Layout.Info() );
			}
			infos.Add( new Styles.ToDeleteOrMove.Info() );

			if( AppRequestState.Instance.Browser.IsFirefox35OrBelow() )
				infos.Add( new Styles.Firefox.Info() );
			if( AppRequestState.Instance.Browser.IsInternetExplorer() )
				infos.Add( new Styles.InternetExplorer.Info() );

			return infos;
		}

		IEnumerable<CssInfo> AppMetaLogicFactory.GetPrintMediaCssInfos() {
			return new Styles.Print.Info().ToSingleElementArray();
		}
	}
}