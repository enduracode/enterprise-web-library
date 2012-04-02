namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Ui.Entity {
	/// <summary>
	/// Overrides the default tab mode.
	/// </summary>
	public interface TabModeOverrider {
		/// <summary>
		/// Gets the tab mode. The default is Automatic. NOTE: The default is Vertical right now in this kick-off period.
		/// </summary>
		TabMode GetTabMode();
	}
}