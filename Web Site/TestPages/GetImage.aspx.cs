using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.WebFileSending;

// Parameter: string text

namespace EnterpriseWebLibrary.WebSite.TestPages {
	public partial class GetImage: EwfPage {
		protected override FileCreator fileCreator { get { return NetTools.CreateImageFromText( info.Text, null ); } }
		protected override bool sendsFileInline { get { return true; } }
	}
}