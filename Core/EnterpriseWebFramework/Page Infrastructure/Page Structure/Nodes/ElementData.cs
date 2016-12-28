using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Data for a page element.
	/// </summary>
	public sealed class ElementData {
		internal readonly IEnumerable<FlowComponentOrNode> Children;
		internal readonly IEnumerable<EtherealComponentOrElement> EtherealChildren;
		internal readonly Func<ElementLocalData> LocalDataGetter;

		/// <summary>
		/// Creates an element-data object.
		/// </summary>
		public ElementData(
			Func<ElementLocalData> localDataGetter, IEnumerable<FlowComponentOrNode> children = null, IEnumerable<EtherealComponentOrElement> etherealChildren = null ) {
			Children = children ?? ImmutableArray<FlowComponentOrNode>.Empty;
			EtherealChildren = etherealChildren ?? ImmutableArray<EtherealComponentOrElement>.Empty;
			LocalDataGetter = localDataGetter;
		}
	}
}