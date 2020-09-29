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
		internal readonly bool IncludeIdAttribute;
		internal readonly string JsInitStatements;

		/// <summary>
		/// Creates an element-node focus-dependent-data object.
		/// </summary>
		public ElementNodeFocusDependentData( IEnumerable<Tuple<string, string>> attributes, bool includeIdAttribute, string jsInitStatements ) {
			if( attributes.Any( i => i.Item1.EqualsIgnoreCase( "id" ) ) )
				throw new ApplicationException( "The framework manages element IDs." );
			Attributes = attributes;

			IncludeIdAttribute = includeIdAttribute;
			JsInitStatements = jsInitStatements;
		}
	}
}