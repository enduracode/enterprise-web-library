using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Data for a particular displayable element, not including its children.
	/// </summary>
	public class DisplayableElementLocalData {
		internal readonly Func<DisplaySetup, ElementLocalData> BaseDataGetter;

		/// <summary>
		/// Creates a local-data object for a nonfocusable displayable element.
		/// </summary>
		public DisplayableElementLocalData( string elementName, DisplayableElementFocusDependentData focusDependentData = null ) {
			BaseDataGetter = displaySetup => new ElementLocalData(
				elementName,
				( focusDependentData ?? new DisplayableElementFocusDependentData() ).BaseDataGetter( displaySetup ) );
		}

		/// <summary>
		/// Creates a local-data object for a focusable displayable element.
		/// </summary>
		public DisplayableElementLocalData(
			string elementName, FocusabilityCondition focusabilityCondition, Func<bool, DisplayableElementFocusDependentData> focusDependentDataGetter ) {
			BaseDataGetter = displaySetup => new ElementLocalData(
				elementName,
				focusabilityCondition,
				isFocused => focusDependentDataGetter( isFocused ).BaseDataGetter( displaySetup ) );
		}
	}
}