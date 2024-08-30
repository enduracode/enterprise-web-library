namespace EnterpriseWebLibrary.Website.Providers;

partial class RequestDispatching {
	protected override SlowRequestThreshold GetSlowRequestThreshold() => SlowRequestThreshold._0500ms;
	protected override IEnumerable<BaseUrlPattern> GetBaseUrlPatterns() => WebFrameworkDemo.EntitySetup.UrlPatterns.BaseUrlPattern().ToCollection();
	public override UrlHandler GetFrameworkUrlParent() => new WebFrameworkDemo.EntitySetup();
}