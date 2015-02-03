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

		IEnumerable<ResourceInfo> AppMetaLogicFactory.GetDisplayMediaCssInfos() {
			var infos = new List<ResourceInfo>();
			infos.Add( new ThirdParty.JqueryUi.Jquery_ui_1104Custom.Css.Custom_theme.Jquery_ui_1104Custommin.Info() );
			infos.Add( new ThirdParty.Select2.Select2_343.Select2.Info() );
			infos.Add( new ThirdParty.TimePicker.Styles.Info() );
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

		IEnumerable<ResourceInfo> AppMetaLogicFactory.GetPrintMediaCssInfos() {
			return new Styles.Print.Info().ToSingleElementArray();
		}
	}
}