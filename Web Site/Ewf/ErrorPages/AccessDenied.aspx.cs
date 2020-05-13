// Parameter: bool showHomeLink

namespace EnterpriseWebLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.ErrorPages {
	partial class AccessDenied: EwfPage {
		partial class Info {
			protected override bool IsIntermediateInstallationPublicResource => true;
			protected override ConnectionSecurity ConnectionSecurity => ConnectionSecurity.MatchingCurrentRequest;
		}

		protected override void loadData() {
			accessDenied.InnerText = Translation.AccessIsDenied;

			if( info.ShowHomeLink )
				ph.AddControlsReturnThis(
					new Paragraph(
							new EwfHyperlink(
								new ExternalResourceInfo( NetTools.HomeUrl ),
								new StandardHyperlinkStyle( Translation.ClickHereToGoToHomePage ) ).ToCollection() )
						.ToCollection()
						.GetControls() );

			Response.StatusCode = 403;
			Response.TrySkipIisCustomErrors = true;
		}
	}
}