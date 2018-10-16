using EnterpriseWebLibrary.EnterpriseWebFramework;

// Parameter: string text

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class GetImage: EwfPage {
		protected override EwfSafeRequestHandler requestHandler => new EwfSafeResponseWriter( NetTools.CreateImageFromText( info.Text, null ) );
	}
}