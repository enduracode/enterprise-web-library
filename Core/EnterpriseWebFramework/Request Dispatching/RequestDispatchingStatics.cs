using System;
using System.Collections.Generic;
using System.Linq;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public static class RequestDispatchingStatics {
		private static Func<AppRequestDispatchingProvider> providerGetter;

		internal static void Init( Func<AppRequestDispatchingProvider> providerGetter ) {
			RequestDispatchingStatics.providerGetter = providerGetter;
		}

		/// <summary>
		/// Framework use only.
		/// </summary>
		public static AppRequestDispatchingProvider AppProvider => providerGetter();

		/// <summary>
		/// Returns a list of URL patterns for the framework.
		/// </summary>
		/// <param name="frameworkUrlSegment">The URL segment that will be a base for the framework’s own pages and resources. Pass the empty string to use the
		/// default of “ewl”. Do not pass null.</param>
		/// <param name="appStaticFileUrlSegment">The URL segment that will be a base for the application’s static files. Pass the empty string to use the default
		/// of “static”. Do not pass null.</param>
		public static IReadOnlyCollection<UrlPattern> GetFrameworkUrlPatterns( string frameworkUrlSegment = "", string appStaticFileUrlSegment = "" ) {
			var patterns = new List<UrlPattern>();

			if( !frameworkUrlSegment.Any() )
				frameworkUrlSegment = EwlStatics.EwlInitialism.ToUrlSlug();
			patterns.Add( new UrlPattern() );

			if( !appStaticFileUrlSegment.Any() )
				appStaticFileUrlSegment = "static";
			patterns.Add( AppProvider.GetStaticFilesFolderUrlPattern( appStaticFileUrlSegment ) );

			return patterns;
		}
	}
}