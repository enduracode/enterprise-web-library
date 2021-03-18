using System.Collections.Generic;
using System.Linq;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public static class RequestDispatchingStatics {
		/// <summary>
		/// Returns the URL patterns for the framework.
		/// </summary>
		/// <param name="adminAreaUrlSegment">The URL segment for the admin area. Pass the empty string to use the default of “ewl”. Do not pass null.</param>
		public static IReadOnlyCollection<UrlPattern> GetFrameworkUrlPatterns( string adminAreaUrlSegment = "" ) {
			var patterns = new List<UrlPattern>();

			if( !adminAreaUrlSegment.Any() )
				adminAreaUrlSegment = EwlStatics.EwlInitialism.ToUrlSlug();
			patterns.Add( new UrlPattern() );

			return patterns;
		}
	}
}