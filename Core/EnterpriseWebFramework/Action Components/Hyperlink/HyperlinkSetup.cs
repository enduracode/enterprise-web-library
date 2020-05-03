using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The configuration for a hyperlink.
	/// </summary>
	public class HyperlinkSetup: ActionComponentSetup {
		private readonly Func<Func<string, ActionComponentIcon, HyperlinkStyle>, PhrasingComponent> hyperlinkGetter;

		/// <summary>
		/// Creates a hyperlink setup object.
		/// </summary>
		/// <param name="behavior">The behavior. Pass a <see cref="ResourceInfo"/> to navigate to the resource in the default way, or call
		/// <see cref="HyperlinkBehaviorExtensionCreators.ToHyperlinkNewTabBehavior(ResourceInfo)"/> or
		/// <see cref="HyperlinkBehaviorExtensionCreators.ToHyperlinkModalBoxBehavior(ResourceInfo, BrowsingContextSetup)"/>. For a mailto link, call
		/// <see cref="HyperlinkBehaviorExtensionCreators.ToHyperlinkBehavior(Email.EmailAddress, string, string, string, string)"/>.</param>
		/// <param name="text">Do not pass null. Pass the empty string to use the destination URL.</param>
		/// <param name="displaySetup"></param>
		/// <param name="icon">The icon.</param>
		public HyperlinkSetup( HyperlinkBehavior behavior, string text, DisplaySetup displaySetup = null, ActionComponentIcon icon = null ) {
			DisplaySetup = displaySetup;
			hyperlinkGetter = hyperlinkStyleSelector =>
				behavior.UserCanNavigateToDestination() ? new EwfHyperlink( behavior, hyperlinkStyleSelector( text, icon ) ) : null;
		}

		/// <inheritdoc/>
		public DisplaySetup DisplaySetup { get; }

		/// <inheritdoc/>
		public PhrasingComponent GetActionComponent(
			Func<string, ActionComponentIcon, HyperlinkStyle> hyperlinkStyleSelector, Func<string, ActionComponentIcon, ButtonStyle> buttonStyleSelector,
			bool enableSubmitButton = false ) =>
			hyperlinkGetter( hyperlinkStyleSelector );
	}
}