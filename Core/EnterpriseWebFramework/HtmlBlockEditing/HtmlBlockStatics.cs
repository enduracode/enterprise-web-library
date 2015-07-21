using System;
using System.Linq;
using System.Text.RegularExpressions;
using EnterpriseWebLibrary.Configuration;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Provides useful constants and methods pertaining to HTML blocks.
	/// </summary>
	public static class HtmlBlockStatics {
		private const string providerName = "HtmlBlockEditing";
		private const string applicationRelativeNonSecureUrlPrefix = "@@nonSecure~";
		private const string applicationRelativeSecureUrlPrefix = "@@secure~";

		private static SystemHtmlBlockEditingProvider provider;

		internal static void Init() {
			provider = ConfigurationStatics.GetSystemLibraryProvider( providerName ) as SystemHtmlBlockEditingProvider;
		}

		internal static SystemHtmlBlockEditingProvider SystemProvider {
			get {
				if( provider == null )
					throw ConfigurationStatics.CreateProviderNotFoundException( providerName );
				return provider;
			}
		}

		/// <summary>
		/// Gets the HTML from the specified HTML block, after decoding intra site URIs.
		/// </summary>
		public static string GetHtml( int htmlBlockId ) {
			var html = SystemProvider.GetHtml( htmlBlockId );
			return GetDecodedHtml( html );
		}

		/// <summary>
		/// Decodes intra site URIs in the specified HTML and returns the result. Use this if you have retrieved HTML from the HTML blocks table without using
		/// GetHtml.
		/// </summary>
		public static string GetDecodedHtml( string encodedHtml ) {
			return decodeIntraSiteUris( encodedHtml );
		}

		/// <summary>
		/// Creates a new HTML block and returns its ID.
		/// </summary>
		public static int CreateHtmlBlock( string html ) {
			return SystemProvider.InsertHtmlBlock( encodeIntraSiteUris( html ) );
		}

		/// <summary>
		/// Updates the HTML in the specified HTML block.
		/// </summary>
		public static void UpdateHtmlBlock( int htmlBlockId, string html ) {
			SystemProvider.UpdateHtml( htmlBlockId, encodeIntraSiteUris( html ) );
		}

		// Do this after all validation so that validation doesn't get confused by our app-relative URL prefix "merge fields". We have seen a system run into
		// problems while doing additional validation to verify that all words preceded by @@ were valid system-specific merge fields; it was mistakenly picking up
		// our app-relative prefixes, thinking that they were merge fields, and complaining that they were not valid.
		private static string encodeIntraSiteUris( string html ) {
			// It's safe to assume that in HTML, <> are used for elements most of the time.
			// The rest of the time, this pattern may match Javascript. However, Javascript will fail
			// the tests following this, so we will still not end up changing something we shouldn't be.
			// .+? Capture everything between < >, non-greedy.
			var htmlTagRegex = new Regex( "<.+?>" );

			// From within the capture of an HTML tag, capture href or src followed by the URL.
			// Group 3 is the capture for the everything between double quotes
			// Group 4 is the capture for the everything between single quotes
			var urlReferenceRegex = new Regex( @"(href|src)=(""(.+?)""|'(.+?)')" );

			// Use case-insensitive search. From http://en.wikipedia.org/wiki/URI_scheme:
			//
			// The scheme name consist of a sequence of characters beginning with a letter and followed by any combination of letters, digits, plus ("+"), period
			// ("."), or hyphen ("-"). Although schemes are case-insensitive, the canonical form is lowercase and documents that specify schemes must do so with
			// lowercase letters.
			var schemeRegex = new Regex( @"^([a-z][a-z0-9+.-]*://|mailto:)", RegexOptions.IgnoreCase );

			// Get everything that looks like an HTML tag
			foreach( Match match in htmlTagRegex.Matches( html ) ) {
				// For each url reference inside that tag e.g. href="", src=''
				foreach( Match urlReference in urlReferenceRegex.Matches( match.Value ) ) {
					// Url inside that reference
					var url = ( urlReference.Groups[ 3 ].Value.Any() ? urlReference.Groups[ 3 ] : urlReference.Groups[ 4 ] ).Value;
					// The URL is definitely relative if it doesn't include a scheme. Skip scheme-less URLs that appear to be merge fields.
					if( !schemeRegex.IsMatch( url ) && !url.StartsWith( "@@" ) ) {
						// Passed all tests. Change this relative URL to an absolute URL.
						var uri = new Uri( new Uri( AppRequestState.Instance.Url ), url );
						html = html.Replace( url, uri.AbsoluteUri );
					}
				}
			}

			// Convert absolute URLs to connection-security-specific application relative URLs
			foreach( var secure in new[] { true, false } ) {
				// Later, we may handle URLs for all web applications in the system rather than just the current one. See the comments in decodeIntraSiteUris.
				var baseUrl =
					ConfigurationStatics.InstallationConfiguration.WebApplications.Single( i => i.Name == ConfigurationStatics.AppName ).DefaultBaseUrl.GetUrlString( secure );

				html = html.Replace( baseUrl, secure ? applicationRelativeSecureUrlPrefix : applicationRelativeNonSecureUrlPrefix );
			}

			return html;
		}

		private static string decodeIntraSiteUris( string html ) {
			foreach( var secure in new[] { true, false } ) {
				// Any kind of relative URL could be a problem in an email message since there is no context. This is one reason we decode to absolute URLs.
				//
				// Our intra-site URI coding does not currently support multiple web applications in a system. If we want to support this, we should probably include
				// the web app name or something in the prefix.
				html = Regex.Replace(
					html,
					secure ? applicationRelativeSecureUrlPrefix : applicationRelativeNonSecureUrlPrefix,
					ConfigurationStatics.InstallationConfiguration.WebApplications.Single().DefaultBaseUrl.GetUrlString( secure ) );
			}
			return html;
		}
	}
}