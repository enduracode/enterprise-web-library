using System.Collections.Generic;
using EnterpriseWebLibrary.EnterpriseWebFramework.Ui;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A class that provides an EWF master page with the information it needs to display entity navigation buttons, action buttons, tabs, etc.
	/// </summary>
	public interface UiEntitySetupBase: EntitySetupBase {
		/// <summary>
		/// Creates and returns navigation button setup information.
		/// </summary>
		IReadOnlyCollection<ActionComponentSetup> GetNavActions();

		/// <summary>
		/// Creates and returns lookup box setup information.
		/// </summary>
		List<LookupBoxSetup> CreateLookupBoxSetups();

		/// <summary>
		/// Creates and returns action button setup information.
		/// </summary>
		IReadOnlyCollection<ActionComponentSetup> GetActions();
	}
}