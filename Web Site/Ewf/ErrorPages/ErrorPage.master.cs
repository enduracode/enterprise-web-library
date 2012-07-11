using System.Web.UI;
using RedStapler.StandardLibrary.DataAccess;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.ErrorPages {
	public partial class ErrorPage: MasterPage, ControlTreeDataLoader {
		protected ErrorPage() {}

		void ControlTreeDataLoader.LoadData( DBConnection cn ) {}
	}
}