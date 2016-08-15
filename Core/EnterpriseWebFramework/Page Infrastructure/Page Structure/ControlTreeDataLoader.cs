namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A control that loads data when it is part of the control tree of an EWF page. The page calls LoadData on these controls in top-down, depth-first order.
	/// </summary>
	public interface ControlTreeDataLoader {
		/// <summary>
		/// Loads and displays data in the control.
		/// </summary>
		void LoadData();
	}
}