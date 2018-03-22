using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Data for a particular element, not including its children.
	/// </summary>
	public class ElementLocalData {
		internal readonly Func<ElementClassSet, ElementNodeLocalData> NodeDataGetter;

		/// <summary>
		/// Creates a local-data object for a nonfocusable element.
		/// </summary>
		public ElementLocalData( string elementName, ElementFocusDependentData focusDependentData = null ) {
			NodeDataGetter = classSet => new ElementNodeLocalData( elementName, ( focusDependentData ?? new ElementFocusDependentData() ).NodeDataGetter( classSet ) );
		}

		/// <summary>
		/// Creates a local-data object for a focusable element.
		/// </summary>
		public ElementLocalData( string elementName, Func<bool, ElementFocusDependentData> focusDependentDataGetter ) {
			NodeDataGetter = classSet => new ElementNodeLocalData( elementName, isFocused => focusDependentDataGetter( isFocused ).NodeDataGetter( classSet ) );
		}
	}
}