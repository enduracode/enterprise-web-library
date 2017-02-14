using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Data for an element node.
	/// </summary>
	internal sealed class ElementNodeData {
		internal readonly IEnumerable<FlowComponentOrNode> Children;
		internal readonly IEnumerable<EtherealComponentOrElement> EtherealChildren;
		internal readonly Func<ElementNodeLocalData> LocalDataGetter;

		/// <summary>
		/// Creates an element-node-data object.
		/// </summary>
		public ElementNodeData(
			Func<ElementNodeLocalData> localDataGetter, IEnumerable<FlowComponentOrNode> children = null, IEnumerable<EtherealComponentOrElement> etherealChildren = null ) {
			Children = children ?? ImmutableArray<FlowComponentOrNode>.Empty;
			EtherealChildren = etherealChildren ?? ImmutableArray<EtherealComponentOrElement>.Empty;
			LocalDataGetter = localDataGetter;
		}
	}
}