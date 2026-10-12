using EnterpriseWebLibrary.Website.StaticFiles;

namespace EnterpriseWebLibrary.Website.Providers;

class StandardPageLogic: AppStandardPageLogicProvider {
	protected override List<ResourceInfo> GetStyleSheets() => new() { new TestCss() };
}