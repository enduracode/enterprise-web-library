#nullable disable
namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// User-interface logic for an entity setup.
	/// </summary>
	public interface UiEntitySetup {
		/// <summary>
		/// Returns the UI setup object for the entity.
		/// </summary>
		EntityUiSetup GetUiSetup();
	}
}