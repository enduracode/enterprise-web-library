using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;

// Parameter: bool showHomeLink

namespace EnterpriseWebLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.ErrorPages {
	partial class AccessDenied: EwfPage {
		partial class Info {
			protected override bool IsIntermediateInstallationPublicResource { get { return true; } }
			protected override ConnectionSecurity ConnectionSecurity { get { return ConnectionSecurity.MatchingCurrentRequest; } }
		}

		protected override void loadData() {
			accessDenied.InnerText = Translation.AccessIsDenied;

			if( info.ShowHomeLink ) {
				ph.AddControlsReturnThis(
					new Paragraph( EwfLink.Create( new ExternalResourceInfo( NetTools.HomeUrl ), new TextActionControlStyle( Translation.ClickHereToGoToHomePage ) ) ) );
			}

			Response.StatusCode = 403;
			Response.TrySkipIisCustomErrors = true;
		}
	}
}