namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The configuration for a radio button.
	/// </summary>
	public class RadioButtonSetup {
		/// <summary>
		/// Creates a setup object for a standard radio button.
		/// </summary>
		/// <param name="displaySetup"></param>
		/// <param name="classes">The classes on the control.</param>
		/// <param name="action">The action that will occur when the user hits Enter on the control. Pass null to use the current default action.</param>
		/// <param name="pageModificationValue"></param>
		public static RadioButtonSetup Create(
			DisplaySetup displaySetup = null, ElementClassSet classes = null, SpecifiedValue<FormAction> action = null,
			PageModificationValue<bool> pageModificationValue = null ) {
			return new RadioButtonSetup( displaySetup, false, classes, action, pageModificationValue );
		}

		/// <summary>
		/// Creates a setup object for a read-only radio button.
		/// </summary>
		/// <param name="displaySetup"></param>
		/// <param name="classes">The classes on the control.</param>
		public static RadioButtonSetup CreateReadOnly( DisplaySetup displaySetup = null, ElementClassSet classes = null ) {
			return new RadioButtonSetup( displaySetup, true, classes, null, null );
		}

		internal readonly DisplaySetup DisplaySetup;
		internal readonly bool IsReadOnly;
		internal readonly ElementClassSet Classes;
		internal readonly FormAction Action;
		internal readonly PageModificationValue<bool> PageModificationValue;

		private RadioButtonSetup(
			DisplaySetup displaySetup, bool isReadOnly, ElementClassSet classes, SpecifiedValue<FormAction> action,
			PageModificationValue<bool> pageModificationValue ) {
			DisplaySetup = displaySetup;
			IsReadOnly = isReadOnly;
			Classes = classes;
			Action = action != null ? action.Value : FormState.Current.FormControlDefaultAction;
			PageModificationValue = pageModificationValue ?? new PageModificationValue<bool>();
		}
	}
}