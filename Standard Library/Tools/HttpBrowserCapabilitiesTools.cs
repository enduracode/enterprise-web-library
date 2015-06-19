using System.Web;

namespace RedStapler.StandardLibrary {
	/// <summary>
	/// Helpful System.Web.HttpBrowserCapabilities methods.
	/// </summary>
	public static class HttpBrowserCapabilitiesTools {
		/// <summary>
		/// Returns true if the user has an old version of Firefox or IE. Chrome updates itself, and users of other browsers like Opera
		/// probably know what they are doing.
		/// </summary>
		public static bool IsOldVersionOfMajorBrowser( this HttpBrowserCapabilities browser ) {
			const int latestIeVersion = 9;
			const int latestFirefoxVersion = 4;
			return ( browser.IsInternetExplorer() && browser.MajorVersion < latestIeVersion ) || ( browser.isFirefox() && browser.MajorVersion < latestFirefoxVersion );
		}

		private static bool isFirefox( this HttpBrowserCapabilities browser ) {
			return browser.Browser == "Firefox";
		}

		/// <summary>
		/// Returns true if the browser being used is any version of Internet Explorer.
		/// </summary>
		public static bool IsInternetExplorer( this HttpBrowserCapabilities browser ) {
			return browser.Browser == "IE";
		}
	}
}