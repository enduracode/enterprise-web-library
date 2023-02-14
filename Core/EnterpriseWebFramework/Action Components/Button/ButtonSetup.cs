namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The configuration for a button.
	/// </summary>
	public class ButtonSetup: ActionComponentSetup {
		private readonly Func<bool, Func<string, ActionComponentIcon, ButtonStyle>, PhrasingComponent> buttonGetter;

		/// <summary>
		/// Creates a button setup object.
		/// </summary>
		/// <param name="text">Do not pass null or the empty string.</param>
		/// <param name="displaySetup"></param>
		/// <param name="behavior">The behavior. Pass null to use the form default action.</param>
		/// <param name="classes">The classes on the button.</param>
		/// <param name="icon">The icon.</param>
		public ButtonSetup(
			string text, DisplaySetup displaySetup = null, ButtonBehavior behavior = null, ElementClassSet classes = null, ActionComponentIcon icon = null ) {
			behavior ??= new FormActionBehavior( FormState.Current.DefaultAction );

			DisplaySetup = displaySetup;
			buttonGetter = ( enableSubmitButton, buttonStyleSelector ) => {
				var postBack = !enableSubmitButton ? null :
				               behavior is FormActionBehavior formActionBehavior ? ( formActionBehavior.Action as PostBackFormAction )?.PostBack :
				               behavior is PostBackBehavior postBackBehavior ? postBackBehavior.PostBackAction.PostBack : null;
				return postBack != null
					       ? new SubmitButton( buttonStyleSelector( text, icon ), classes: classes, postBack: postBack )
					       : new EwfButton( buttonStyleSelector( text, icon ), behavior: behavior, classes: classes );
			};
		}

		/// <inheritdoc/>
		public DisplaySetup DisplaySetup { get; }

		/// <inheritdoc/>
		public PhrasingComponent GetActionComponent(
			Func<string, ActionComponentIcon, HyperlinkStyle> hyperlinkStyleSelector, Func<string, ActionComponentIcon, ButtonStyle> buttonStyleSelector,
			bool enableSubmitButton = false ) =>
			buttonGetter( enableSubmitButton, buttonStyleSelector );
	}
}