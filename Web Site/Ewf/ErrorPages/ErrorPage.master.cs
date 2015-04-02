using System.Web.UI;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.ErrorPages {
	public partial class ErrorPage: MasterPage, ControlTreeDataLoader {
		protected ErrorPage() {}

		void ControlTreeDataLoader.LoadData() {
			BasicPage.Instance.Body.Attributes[ "class" ] = CssElementCreator.ErrorPageBodyCssClass;
		}
	}
}