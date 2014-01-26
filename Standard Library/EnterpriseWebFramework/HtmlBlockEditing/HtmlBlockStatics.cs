using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
    /// <summary>
    /// Provides useful constants and methods pertaining to HTML blocks.
    /// </summary>
    public static class HtmlBlockStatics {
        private const string providerName = "HtmlBlockEditing";
        private const string applicationRelativeNonSecureUrlPrefix = "@@nonSecure~";
        private const string applicationRelativeSecureUrlPrefix = "@@secure~";

        private static SystemHtmlBlockEditingProvider provider;

        internal static void Init( Type systemLogicType ) {
            provider = StandardLibraryMethods.GetSystemLibraryProvider( systemLogicType, providerName ) as SystemHtmlBlockEditingProvider;
        }

        internal static SystemHtmlBlockEditingProvider SystemProvider {
            get {
                if( provider == null )
                    throw StandardLibraryMethods.CreateProviderNotFoundException( providerName );
                return provider;
            }
        }

        /// <summary>
        /// Gets the HTML from the specified HTML block, after decoding intra site URIs.
        /// NOTE: Do not use the nonSecureBaseUrl or secureBaseUrl parameters until you read the NOTE on decodeIntraSiteUris in this class.
        /// </summary>
        public static string GetHtml( int htmlBlockId, string nonSecureBaseUrl = "", string secureBaseUrl = "" ) {
            var html = SystemProvider.GetHtml( htmlBlockId ) ?? ""; // NOTE: Why does GetHtml ever return null?
            return GetDecodedHtml( html, nonSecureBaseUrl: nonSecureBaseUrl, secureBaseUrl: secureBaseUrl );
        }

        /// <summary>
        /// Decodes intra site URIs in the specified HTML and returns the result. Use this if you have retrieved HTML from the HTML blocks table without using
        /// GetHtml.
        /// NOTE: Do not use the nonSecureBaseUrl or secureBaseUrl parameters until you read the NOTE on decodeIntraSiteUris in this class.
        /// </summary>
        public static string GetDecodedHtml( string encodedHtml, string nonSecureBaseUrl = "", string secureBaseUrl = "" ) {
            return decodeIntraSiteUris( encodedHtml, nonSecureBaseUrl, secureBaseUrl );
        }

        internal static string EncodeIntraSiteUris( string html ) {
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
                var baseUrl = EwfApp.GetBaseUrlWithSpecificSecurity( secure );
                html = html.Replace( baseUrl, secure ? applicationRelativeSecureUrlPrefix : applicationRelativeNonSecureUrlPrefix );
            }

            return html;
        }

        // NOTE: The nonSecureBaseUrl and secureBaseUrl parameters are a hack. See https://info.redstapler.biz/Pages/Goal/EditGoal.aspx?GoalId=567.
        private static string decodeIntraSiteUris( string html, string nonSecureBaseUrl, string secureBaseUrl ) {
            foreach( var secure in new[] { true, false } ) {
                // Any kind of relative URL could be a problem in an email message since there is no context. This is one reason we decode to absolute URLs.
                html = Regex.Replace( html,
                                      secure ? applicationRelativeSecureUrlPrefix : applicationRelativeNonSecureUrlPrefix,
                                      secure
                                          ? secureBaseUrl.Any() ? secureBaseUrl : EwfApp.GetBaseUrlWithSpecificSecurity( true )
                                          : nonSecureBaseUrl.Any() ? nonSecureBaseUrl : EwfApp.GetBaseUrlWithSpecificSecurity( false ) );
            }
            return html;
        }
    }
}