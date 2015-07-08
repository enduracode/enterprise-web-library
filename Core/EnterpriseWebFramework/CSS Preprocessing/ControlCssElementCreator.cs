namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An object that defines one or more CSS elements.
	/// </summary>
	public interface ControlCssElementCreator {
		/// <summary>
		/// Creates an array of CSS elements.
		/// </summary>
		CssElement[] CreateCssElements();
	}
}