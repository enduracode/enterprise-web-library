#nullable disable
namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public class PageReloadBehavior {
		internal string FocusKey { get; }
		internal SecondaryResponse SecondaryResponse { get; }

		/// <summary>
		/// Creates a behavior.
		/// </summary>
		/// <param name="focusKey">The focus key, which you can use to autofocus on a region of the reloaded page by referencing the key from autofocus conditions.
		/// Do not pass null. Pass the empty string for no autofocus.</param>
		/// <param name="secondaryResponse">A secondary response, which will load in a new window/tab or as an attachment.</param>
		public PageReloadBehavior( string focusKey = "", SecondaryResponse secondaryResponse = null ) {
			FocusKey = focusKey;
			SecondaryResponse = secondaryResponse;
		}
	}
}