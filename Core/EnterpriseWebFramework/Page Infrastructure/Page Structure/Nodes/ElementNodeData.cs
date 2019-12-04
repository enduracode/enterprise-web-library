using System;
using System.Collections.Generic;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Data for an element node.
	/// </summary>
	internal sealed class ElementNodeData {
		internal readonly IReadOnlyCollection<FlowComponentOrNode> Children;
		internal readonly IReadOnlyCollection<EtherealComponentOrElement> EtherealChildren;
		internal readonly Func<ElementNodeLocalData> LocalDataGetter;

		/// <summary>
		/// Creates an element-node-data object.
		/// </summary>
		public ElementNodeData(
			Func<ElementNodeLocalData> localDataGetter, IReadOnlyCollection<FlowComponentOrNode> children = null,
			IReadOnlyCollection<EtherealComponentOrElement> etherealChildren = null ) {
			Children = children ?? Enumerable.Empty<FlowComponentOrNode>().Materialize();
			EtherealChildren = etherealChildren ?? Enumerable.Empty<EtherealComponentOrElement>().Materialize();
			LocalDataGetter = localDataGetter;
		}
	}
}