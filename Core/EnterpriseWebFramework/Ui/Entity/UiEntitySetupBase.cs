using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A class that provides an EWF master page with the information it needs to display entity navigation buttons, action buttons, tabs, etc.
	/// </summary>
	public interface UiEntitySetupBase: EntitySetupBase {
		/// <summary>
		/// Gets the navigational action components.
		/// </summary>
		IReadOnlyCollection<ActionComponentSetup> GetNavActions();

		/// <summary>
		/// Gets the navigational form controls.
		/// </summary>
		IReadOnlyCollection<NavFormControl> GetNavFormControls();

		/// <summary>
		/// Gets the action components.
		/// </summary>
		IReadOnlyCollection<ActionComponentSetup> GetActions();
	}
}