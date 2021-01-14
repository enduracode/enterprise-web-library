using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Focus-dependent data for a displayable element.
	/// </summary>
	public class DisplayableElementFocusDependentData {
		internal readonly Func<DisplaySetup, ElementFocusDependentData> BaseDataGetter;

		/// <summary>
		/// Creates a displayable-element focus-dependent-data object.
		/// </summary>
		public DisplayableElementFocusDependentData(
			IEnumerable<ElementAttribute> attributes = null, bool includeIdAttribute = false, string jsInitStatements = "" ) {
			BaseDataGetter = displaySetup => new ElementFocusDependentData(
				( attributes ?? ImmutableArray<ElementAttribute>.Empty ).Concat(
					!displaySetup.ComponentsDisplayed ? new ElementAttribute( "style", "display: none" ).ToCollection() : ImmutableArray<ElementAttribute>.Empty ),
				displaySetup.UsesJsStatements || includeIdAttribute,
				jsInitStatements );
		}
	}
}