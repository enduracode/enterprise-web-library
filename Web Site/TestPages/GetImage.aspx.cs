using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.WebFileSending;

// Parameter: string text

namespace EnterpriseWebLibrary.WebSite.TestPages {
	public partial class GetImage: EwfPage {
		public partial class Info {
			protected override void init( DBConnection cn ) {}
		}

		protected override FileCreator fileCreator { get { return NetTools.CreateImageFromText( info.Text, null ); } }

		protected override bool sendsFileInline { get { return true; } }
		protected override void LoadData( DBConnection cn ) {}
	}
}