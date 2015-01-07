using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.EnterpriseWebFramework;

// Parameter: string text

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class GetImage: EwfPage {
		protected override EwfSafeResponseWriter responseWriter { get { return new EwfSafeResponseWriter( NetTools.CreateImageFromText( info.Text, null ) ); } }
	}
}