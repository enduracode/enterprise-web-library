using System.Web;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A base set of functionality that can be used to discover information about a CSS file before actually requesting it.
	/// </summary>
	public abstract class CssInfo {
		/// <summary>
		/// Returns an app relative URL for the CSS file.
		/// </summary>
		public abstract string GetUrl();

		/// <summary>
		/// Gets the path of the CSS file.
		/// </summary>
		internal string FilePath { get { return StandardLibraryMethods.CombinePaths( HttpRuntime.AppDomainAppPath, appRelativeFilePath ); } }

		/// <summary>
		/// Gets the app relative path of the CSS file.
		/// </summary>
		protected abstract string appRelativeFilePath { get; }
	}
}