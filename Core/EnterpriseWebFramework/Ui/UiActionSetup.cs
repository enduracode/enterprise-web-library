using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public interface UiActionSetup {
		/// <summary>
		/// EWF use only.
		/// </summary>
		PhrasingComponent GetActionComponent(
			Func<string, ActionComponentIcon, HyperlinkStyle> hyperlinkStyleSelector, Func<string, ActionComponentIcon, ButtonStyle> buttonStyleSelector,
			bool enableSubmitButton = false );
	}
}