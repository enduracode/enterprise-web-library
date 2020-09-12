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
			IEnumerable<Tuple<string, string>> attributes = null, bool includeIdAttribute = false, string jsInitStatements = "" ) {
			BaseDataGetter = displaySetup => new ElementFocusDependentData(
				( attributes ?? ImmutableArray<Tuple<string, string>>.Empty ).Concat(
					!displaySetup.ComponentsDisplayed ? Tuple.Create( "style", "display: none" ).ToCollection() : ImmutableArray<Tuple<string, string>>.Empty ),
				displaySetup.UsesJsStatements || includeIdAttribute,
				jsInitStatements );
		}
	}
}