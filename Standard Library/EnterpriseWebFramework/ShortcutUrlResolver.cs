using System;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A function that can resolve a particular shortcut URL into a page.
	/// </summary>
	public class ShortcutUrlResolver {
		internal string ShortcutUrl { get; private set; }
		internal ConnectionSecurity ConnectionSecurity { get; private set; }
		internal Func<PageInfo> Function { get; private set; }
		internal Func<PageInfo> LogInPageGetter { get; private set; }

		/// <summary>
		/// Creates a shortcut URL resolver. The shortcut URL parameter is the application relative URL that this resolver will handle. For example, if the URL of
		/// the application is "integration.redstapler.biz/Todd" and you want to handle requests for "integration.redstapler.biz/Todd/admin", specify "admin"; to
		/// handle requests for the application root, specify the empty string. For a non-root URL, if you want it to have a trailing slash, you must include this
		/// slash at the end of the string. The specified function will be called when a request for the shortcut URL comes in and should return the page you want
		/// to display. The page may vary depending on the authenticated user, and returning null will display the Access Denied page. Do not return a page that the
		/// authenticated user cannot access.
		/// </summary>
		public ShortcutUrlResolver( string shortcutUrl, ConnectionSecurity connectionSecurity, Func<PageInfo> function, Func<PageInfo> logInPageGetter = null ) {
			ShortcutUrl = shortcutUrl;
			ConnectionSecurity = connectionSecurity;
			Function = function;
			LogInPageGetter = logInPageGetter;
		}
	}
}