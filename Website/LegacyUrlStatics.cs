using System.Collections.Generic;
using EnterpriseWebLibrary.EnterpriseWebFramework;

namespace EnterpriseWebLibrary.WebSite {
	internal static class LegacyUrlStatics {
		public static IReadOnlyCollection<UrlPattern> GetPatterns() {
			var patterns = new List<UrlPattern>();
			patterns.Add( TestPages.LegacyUrlFolderSetup.UrlPatterns.Literal( "TestPages" ) );
			return patterns;
		}

		public static UrlHandler GetParent() => new TestPages.EntitySetup();
	}
}