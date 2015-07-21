using System.Web.UI;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.ErrorPages {
	public partial class ErrorPage: MasterPage, ControlTreeDataLoader {
		protected ErrorPage() {}

		void ControlTreeDataLoader.LoadData() {
			BasicPage.Instance.Body.Attributes[ "class" ] = CssElementCreator.ErrorPageBodyCssClass;
		}
	}
}