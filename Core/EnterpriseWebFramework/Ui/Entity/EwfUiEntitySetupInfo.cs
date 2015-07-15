using EnterpriseWebLibrary.EnterpriseWebFramework.Ui.Entity;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A base set of functionality that can be used to discover information about an EWF-UI entity setup before requesting a resource that uses it.
	/// </summary>
	public abstract class EwfUiEntitySetupInfo: EntitySetupInfo {
		/// <summary>
		/// Gets the tab mode. The default is Automatic.
		/// </summary>
		public virtual TabMode GetTabMode() {
			return TabMode.Automatic;
		}
	}
}