#nullable disable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Focus-dependent data for an element.
	/// </summary>
	public class ElementFocusDependentData {
		internal readonly Func<ElementClassSet, ElementNodeFocusDependentData> NodeDataGetter;

		/// <summary>
		/// Creates an element focus-dependent-data object.
		/// </summary>
		public ElementFocusDependentData( IEnumerable<ElementAttribute> attributes = null, bool includeIdAttribute = false, string jsInitStatements = "" ) {
			NodeDataGetter = classSet => {
				var classValue = StringTools.ConcatenateWithDelimiter( " ", classSet.GetClassNames().ToArray() );
				return new ElementNodeFocusDependentData(
					( classValue.Any() ? new ElementAttribute( "class", classValue ).ToCollection() : ImmutableArray<ElementAttribute>.Empty ).Concat(
						attributes ?? ImmutableArray<ElementAttribute>.Empty ),
					classSet.UsesElementIds || includeIdAttribute,
					jsInitStatements );
			};
		}
	}
}