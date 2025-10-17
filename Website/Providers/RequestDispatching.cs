using Microsoft.AspNetCore.Builder;

namespace EnterpriseWebLibrary.Website.Providers;

partial class RequestDispatching {
	protected override void ConfigurePostFrameworkPipeline( WebApplication app ) {
		app.MapGet( "asp-net/minimal-api", WebFrameworkDemo.AspNet.ApiMethods.Get );

		app.UseHttpsRedirection();
		app.MapControllers();
	}

	protected override SlowRequestThreshold GetSlowRequestThreshold() => SlowRequestThreshold._0500ms;
	protected override IEnumerable<BaseUrlPattern> GetBaseUrlPatterns() => WebFrameworkDemo.EntitySetup.UrlPatterns.BaseUrlPattern().ToCollection();
	public override UrlHandler GetFrameworkUrlParent() => new WebFrameworkDemo.EntitySetup();
}