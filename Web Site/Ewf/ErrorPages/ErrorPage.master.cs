using System.Web.UI;
using RedStapler.StandardLibrary.DataAccess;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.RedStapler.TestWebSite.Ewf.ErrorPages {
	public partial class ErrorPage: MasterPage, ControlTreeDataLoader {
		protected ErrorPage() {}

		void ControlTreeDataLoader.LoadData( DBConnection cn ) {}
	}
}