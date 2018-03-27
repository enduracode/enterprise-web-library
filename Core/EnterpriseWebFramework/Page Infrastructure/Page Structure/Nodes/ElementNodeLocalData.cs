using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Data for a particular element node, not including its children.
	/// </summary>
	internal sealed class ElementNodeLocalData {
		internal readonly string ElementName;
		internal readonly FocusabilityCondition FocusabilityCondition;
		internal readonly Func<bool, ElementNodeFocusDependentData> FocusDependentDataGetter;

		/// <summary>
		/// Creates a local-data object for a nonfocusable element node.
		/// </summary>
		public ElementNodeLocalData( string elementName, ElementNodeFocusDependentData focusDependentData ) {
			ElementName = elementName;
			FocusabilityCondition = new FocusabilityCondition( false );
			FocusDependentDataGetter = isFocused => focusDependentData;
		}

		/// <summary>
		/// Creates a local-data object for a focusable element node.
		/// </summary>
		internal ElementNodeLocalData(
			string elementName, FocusabilityCondition focusabilityCondition, Func<bool, ElementNodeFocusDependentData> focusDependentDataGetter ) {
			ElementName = elementName;
			FocusabilityCondition = focusabilityCondition;
			FocusDependentDataGetter = focusDependentDataGetter;
		}
	}
}