namespace EnterpriseWebLibrary.Website;

internal static class LegacyUrlStatics {
	public static IReadOnlyCollection<UrlPattern> GetPatterns() {
		var patterns = new List<UrlPattern>();
		patterns.Add( WebFrameworkDemo.LegacyUrlFolderSetup.UrlPatterns.Literal( "TestPages" ) );
		return patterns;
	}

	public static UrlHandler GetParent() => new WebFrameworkDemo.EntitySetup();
}