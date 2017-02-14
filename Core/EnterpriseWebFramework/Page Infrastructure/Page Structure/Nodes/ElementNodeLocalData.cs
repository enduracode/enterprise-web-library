using System;
using System.Collections.Generic;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Data for a particular element node, not including its children.
	/// </summary>
	internal sealed class ElementNodeLocalData {
		internal readonly string ElementName;
		internal readonly IEnumerable<Tuple<string, string>> Attributes;
		internal readonly string Id;
		internal readonly string JsInitStatements;

		/// <summary>
		/// Creates an element-node-local-data object.
		/// </summary>
		public ElementNodeLocalData( string elementName, IEnumerable<Tuple<string, string>> attributes, bool includeIdAttribute, string jsInitStatements )
			: this( elementName, attributes, includeIdAttribute ? "" : null, jsInitStatements ) {}

		/// <summary>
		/// FragmentMarker use only.
		/// </summary>
		internal ElementNodeLocalData( string elementName, IEnumerable<Tuple<string, string>> attributes, string id, string jsInitStatements ) {
			ElementName = elementName;

			if( attributes.Any( i => i.Item1.EqualsIgnoreCase( "id" ) ) )
				throw new ApplicationException( "The framework manages element IDs." );
			Attributes = attributes;

			Id = id;
			JsInitStatements = jsInitStatements;
		}
	}
}