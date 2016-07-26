using System;
using System.Collections.Generic;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Data for a particular page element, not including its children.
	/// </summary>
	public sealed class ElementLocalData {
		internal readonly string ElementName;
		internal readonly IEnumerable<Tuple<string, string>> Attributes;
		internal readonly bool IncludeIdAttribute;
		internal readonly string JsInitStatements;

		/// <summary>
		/// Creates an element-local-data object.
		/// </summary>
		public ElementLocalData( string elementName, IEnumerable<Tuple<string, string>> attributes, bool includeIdAttribute, string jsInitStatements ) {
			ElementName = elementName;

			if( attributes.Any( i => i.Item1.EqualsIgnoreCase( "id" ) ) )
				throw new ApplicationException( "The framework manages element IDs." );
			Attributes = attributes;

			IncludeIdAttribute = includeIdAttribute;
			JsInitStatements = jsInitStatements;
		}
	}
}