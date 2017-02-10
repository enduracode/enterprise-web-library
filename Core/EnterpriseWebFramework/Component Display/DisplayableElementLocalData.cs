using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Data for a particular displayable element, not including its children.
	/// </summary>
	public class DisplayableElementLocalData {
		internal readonly string ElementName;
		internal readonly IEnumerable<Tuple<string, string>> Attributes;
		internal readonly bool IncludeIdAttribute;
		internal readonly string JsInitStatements;

		/// <summary>
		/// Creates a displayable-element-local-data object.
		/// </summary>
		public DisplayableElementLocalData(
			string elementName, ElementClassSet classes = null, IEnumerable<Tuple<string, string>> additionalAttributes = null, bool includeIdAttribute = false,
			string jsInitStatements = "" ) {
			ElementName = elementName;
			Attributes =
				( classes != null
					  ? Tuple.Create( "class", StringTools.ConcatenateWithDelimiter( " ", classes.ClassNames.ToArray() ) ).ToCollection()
					  : ImmutableArray<Tuple<string, string>>.Empty ).Concat( additionalAttributes ?? ImmutableArray<Tuple<string, string>>.Empty );
			IncludeIdAttribute = includeIdAttribute;
			JsInitStatements = jsInitStatements;
		}
	}
}