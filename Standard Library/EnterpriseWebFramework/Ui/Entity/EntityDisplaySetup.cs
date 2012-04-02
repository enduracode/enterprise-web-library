using System.Collections.Generic;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayElements.Entity {
	/// <summary>
	/// A class that provides an EWF master page with the information it needs to display entity navigation buttons, action buttons, tabs, etc.
	/// </summary>
	public interface EntityDisplaySetup: EntitySetupBase {
		/// <summary>
		/// Creates and returns navigation button setup information.
		/// </summary>
		List<ActionButtonSetup> CreateNavButtonSetups();

		/// <summary>
		/// Creates and returns lookup box setup information.
		/// </summary>
		List<LookupBoxSetup> CreateLookupBoxSetups();

		/// <summary>
		/// Creates and returns action button setup information.
		/// </summary>
		List<ActionButtonSetup> CreateActionButtonSetups();
	}
}