using System.Linq;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework;

namespace EnterpriseWebLibrary.WebSite.TestPages.SubFolder {
	public partial class General: EwfPage {
		public partial class Info {
			protected override void init( DBConnection cn ) { }
		}

		protected override void LoadData( DBConnection cn ) { }
	}
}