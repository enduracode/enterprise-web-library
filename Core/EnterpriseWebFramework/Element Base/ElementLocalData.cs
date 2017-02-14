using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Data for a particular element, not including its children.
	/// </summary>
	public class ElementLocalData {
		internal readonly Func<ElementClassSet, ElementNodeLocalData> NodeDataGetter;

		/// <summary>
		/// Creates an element-local-data object.
		/// </summary>
		public ElementLocalData(
			string elementName, IEnumerable<Tuple<string, string>> attributes = null, bool includeIdAttribute = false, string jsInitStatements = "" ) {
			NodeDataGetter = classSet => {
				var classValue = StringTools.ConcatenateWithDelimiter( " ", classSet.GetClassNames().ToArray() );
				return new ElementNodeLocalData(
					elementName,
					( classValue.Any() ? Tuple.Create( "class", classValue ).ToCollection() : ImmutableArray<Tuple<string, string>>.Empty ).Concat(
						attributes ?? ImmutableArray<Tuple<string, string>>.Empty ),
					classSet.UsesElementIds || includeIdAttribute,
					jsInitStatements );
			};
		}
	}
}