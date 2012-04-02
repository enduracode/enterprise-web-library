namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Ui.Entity {
	/// <summary>
	/// Affects how tabs (if any) are displayed.
	/// </summary>
	public enum TabMode {
		/// <summary>
		/// Intelligently decides whether to use horizontal or vertical tabs based on the number of tabs and the presence of tab groups.
		/// </summary>
		Automatic,
		/// <summary>
		/// Forces horizontal tabs. Cannot be used when tab groups exist.
		/// </summary>
		Horizontal,
		/// <summary>
		/// Forces vertical tabs.
		/// </summary>
		Vertical
	}
}