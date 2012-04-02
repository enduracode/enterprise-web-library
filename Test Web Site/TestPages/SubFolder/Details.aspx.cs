using System.Linq;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework;

namespace RedStapler.TestWebSite.TestPages.SubFolder {
	public partial class Details: EwfPage {
		public partial class Info {
			protected override void init( DBConnection cn ) { }
		}

		protected override void LoadData( DBConnection cn ) { }
	}
}