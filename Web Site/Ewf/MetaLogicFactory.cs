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

		IEnumerable<CssInfo> AppMetaLogicFactory.GetDisplayMediaCssInfos() {
			var infos = new List<CssInfo>();
			infos.Add( new JqueryUi.Css.Custom_theme.Jquery_ui_1101Custommin.Info() );
			infos.Add( new Chosen.Chosen.Info() );
			infos.Add( new TimePicker.Styles.Info() );
			infos.Add( new Qtip2.Jqueryqtipmin.Info() );
			infos.Add( new Styles.Basic.Info() );
			infos.Add( new Styles.EwfControls.Info() );
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