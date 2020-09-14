using System;
using System.Collections.Generic;
using System.Linq;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Focus-dependent data for an element node.
	/// </summary>
	internal sealed class ElementNodeFocusDependentData {
		internal readonly IEnumerable<Tuple<string, string>> Attributes;
		internal readonly string Id;
		internal readonly string JsInitStatements;

		/// <summary>
		/// Creates an element-node focus-dependent-data object.
		/// </summary>
		public ElementNodeFocusDependentData( IEnumerable<Tuple<string, string>> attributes, bool includeIdAttribute, string jsInitStatements ): this(
			attributes,
			includeIdAttribute ? "" : null,
			jsInitStatements ) {}

		/// <summary>
		/// FragmentMarker use only.
		/// </summary>
		internal ElementNodeFocusDependentData( IEnumerable<Tuple<string, string>> attributes, string id, string jsInitStatements ) {
			if( attributes.Any( i => i.Item1.EqualsIgnoreCase( "id" ) ) )
				throw new ApplicationException( "The framework manages element IDs." );
			Attributes = attributes;

			Id = id;
			JsInitStatements = jsInitStatements;
		}
	}
}