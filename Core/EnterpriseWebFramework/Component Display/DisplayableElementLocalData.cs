using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Data for a particular displayable element, not including its children.
	/// </summary>
	public class DisplayableElementLocalData {
		internal readonly Func<DisplaySetup, ElementLocalData> BaseDataGetter;

		/// <summary>
		/// Creates a displayable-element-local-data object.
		/// </summary>
		public DisplayableElementLocalData(
			string elementName, IEnumerable<Tuple<string, string>> attributes = null, bool includeIdAttribute = false, string jsInitStatements = "" ) {
			BaseDataGetter =
				displaySetup =>
				new ElementLocalData(
					elementName,
					( attributes ?? ImmutableArray<Tuple<string, string>>.Empty ).Concat(
						!displaySetup.ComponentsDisplayed ? Tuple.Create( "style", "display: none" ).ToCollection() : ImmutableArray<Tuple<string, string>>.Empty ),
					displaySetup.UsesJsStatements || includeIdAttribute,
					jsInitStatements );
		}
	}
}