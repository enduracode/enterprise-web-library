using System.Collections.Generic;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.WebSite.StaticFiles;

namespace EnterpriseWebLibrary.WebSite.Providers {
	internal class StandardPageLogic: AppStandardPageLogicProvider {
		protected override List<ResourceInfo> GetStyleSheets() => new List<ResourceInfo> { new TestCss() };
	}
}