using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The configuration for an action component.
	/// </summary>
	public interface ActionComponentSetup {
		/// <summary>
		/// EWF use only.
		/// </summary>
		PhrasingComponent GetActionComponent(
			Func<string, ActionComponentIcon, HyperlinkStyle> hyperlinkStyleSelector, Func<string, ActionComponentIcon, ButtonStyle> buttonStyleSelector,
			bool enableSubmitButton = false );
	}
}