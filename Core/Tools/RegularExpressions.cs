using System.Text.RegularExpressions;

namespace EnterpriseWebLibrary {
	/// <summary>
	/// Contains common regular expression strings.
	/// </summary>
	public class RegularExpressions {
		/// <summary>
		/// This regex will match anything inside and including &lt;&gt; brackets. %lt;/?\w+((\s+\w+(\s*=\s*(?:".*?"|'.*?'|[^'"&gt;\s]+))?)+\s*|\s*)/?&gt;
		/// </summary>
		public const string HtmlTag = @"</?\w+((\s+\w+(\s*=\s*(?:"".*?""|'.*?'|[^'"">\s]+))?)+\s*|\s*)/?>";

		/// <summary>
		/// Returns the given source string without /**/ comments.
		/// </summary>
		public static string RemoveMultiLineCStyleComments( string source ) {
			return Regex.Replace( source, @"/\*.*?\*/", "", RegexOptions.Singleline );
		}
	}
}