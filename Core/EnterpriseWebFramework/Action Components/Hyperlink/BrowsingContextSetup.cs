namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The configuration for an iframe element.
	/// </summary>
	public class BrowsingContextSetup {
		internal readonly CssLength Width;
		internal readonly CssLength Height;

		/// <summary>
		/// Creates a browsing-context setup object.
		/// </summary>
		/// <param name="width">The width of the iframe.</param>
		/// <param name="height">The height of the iframe.</param>
		public BrowsingContextSetup( ContentBasedLength width = null, ContentBasedLength height = null ) {
			Width = width;
			Height = height;
		}
	}
}