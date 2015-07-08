namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A desired location on a web page for a browser to scroll to.
	/// </summary>
	public enum ScrollPosition {
		/// <summary>
		/// The top left corner of the page. This is the standard behavior that users expect when browsing the web.
		/// </summary>
		TopLeft,

		/// <summary>
		/// The position the user was at when the page was posted back. If this request is not a post back or if there are status messages, this setting is
		/// equivalent to TopLeft.
		/// </summary>
		LastPositionOrStatusBar
	}
}