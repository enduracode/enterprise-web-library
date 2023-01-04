using EnterpriseWebLibrary.Website.StaticFiles;

namespace EnterpriseWebLibrary.Website.Providers {
	internal class StandardPageLogic: AppStandardPageLogicProvider {
		protected override List<ResourceInfo> GetStyleSheets() => new() { new TestCss() };
	}
}