using System;
using System.Collections.Generic;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Data for an element node.
	/// </summary>
	internal sealed class ElementNodeData {
		internal readonly IEnumerable<FlowComponentOrNode> Children;
		internal readonly IEnumerable<EtherealComponent> EtherealChildren;
		internal readonly Func<ElementNodeLocalData> LocalDataGetter;

		/// <summary>
		/// Creates an element-node-data object.
		/// </summary>
		public ElementNodeData(
			Func<ElementNodeLocalData> localDataGetter, IEnumerable<FlowComponentOrNode> children = null, IEnumerable<EtherealComponent> etherealChildren = null ) {
			Children = children ?? Enumerable.Empty<FlowComponentOrNode>();
			EtherealChildren = etherealChildren ?? Enumerable.Empty<EtherealComponent>();
			LocalDataGetter = localDataGetter;
		}
	}
}