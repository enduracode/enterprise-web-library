using EnterpriseWebLibrary.EnterpriseWebFramework;

namespace EnterpriseWebLibrary.WebSite.Providers {
	partial class RequestDispatching {
		public override UrlHandler GetFrameworkUrlParent() => new TestPages.EntitySetup();
	}
}