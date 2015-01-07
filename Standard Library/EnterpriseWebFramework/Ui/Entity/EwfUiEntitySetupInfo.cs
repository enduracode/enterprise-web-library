using RedStapler.StandardLibrary.EnterpriseWebFramework.Ui.Entity;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A base set of functionality that can be used to discover information about an EWF-UI entity setup before requesting a resource that uses it.
	/// </summary>
	public abstract class EwfUiEntitySetupInfo: EntitySetupInfo {
		/// <summary>
		/// Gets the tab mode. The default is Automatic. NOTE: The default is Vertical right now in this kick-off period.
		/// </summary>
		public virtual TabMode GetTabMode() {
			return TabMode.Vertical;
		}
	}
}