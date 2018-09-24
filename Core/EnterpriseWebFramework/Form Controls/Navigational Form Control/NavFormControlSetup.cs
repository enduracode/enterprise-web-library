namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The configuration for a navigational form control.
	/// </summary>
	public class NavFormControlSetup {
		public ContentBasedLength Width { get; }
		public string Placeholder { get; }
		public ResourceInfo AutoCompleteResource { get; }

		/// <summary>
		/// Creates a navigational-form-control setup object.
		/// </summary>
		/// <param name="width">The width of the control. Do not pass null.</param>
		/// <param name="placeholder">The hint word or phrase that will appear when the control has an empty value. Do not pass null or the empty string.</param>
		/// <param name="autoCompleteResource">The resource containing the auto-complete items. Do not pass null.</param>
		public NavFormControlSetup( ContentBasedLength width, string placeholder, ResourceInfo autoCompleteResource = null ) {
			Width = width;
			Placeholder = placeholder;
			AutoCompleteResource = autoCompleteResource;
		}
	}
}