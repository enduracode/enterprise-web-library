namespace @@BaseNamespace.Website.Providers;

partial class RequestDispatching {
	protected override IEnumerable<BaseUrlPattern> GetBaseUrlPatterns() => Home.UrlPatterns.BaseUrlPattern().ToCollection();
	public override UrlHandler GetFrameworkUrlParent() => new Home();
}