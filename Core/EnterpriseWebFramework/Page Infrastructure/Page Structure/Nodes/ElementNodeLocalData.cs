using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Data for a particular element node, not including its children.
	/// </summary>
	internal sealed class ElementNodeLocalData {
		internal readonly string ElementName;
		internal readonly bool IsFocusable;
		internal readonly Func<bool, ElementNodeFocusDependentData> FocusDependentDataGetter;

		/// <summary>
		/// Creates a local-data object for a nonfocusable element node.
		/// </summary>
		public ElementNodeLocalData( string elementName, ElementNodeFocusDependentData focusDependentData ) {
			ElementName = elementName;
			IsFocusable = false;
			FocusDependentDataGetter = isFocused => focusDependentData;
		}

		/// <summary>
		/// Creates a local-data object for a focusable element node.
		/// </summary>
		internal ElementNodeLocalData( string elementName, Func<bool, ElementNodeFocusDependentData> focusDependentDataGetter ) {
			ElementName = elementName;
			IsFocusable = true;
			FocusDependentDataGetter = focusDependentDataGetter;
		}
	}
}