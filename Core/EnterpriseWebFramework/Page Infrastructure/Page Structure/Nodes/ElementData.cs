using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Data for a page element.
	/// </summary>
	public sealed class ElementData {
		internal readonly IEnumerable<FlowComponent> Children;
		internal readonly IEnumerable<EtherealComponent> EtherealChildren;
		internal readonly Func<ElementLocalData> LocalDataGetter;

		/// <summary>
		/// Creates an element-data object.
		/// </summary>
		public ElementData(
			Func<ElementLocalData> localDataGetter, IEnumerable<FlowComponent> children = null, IEnumerable<EtherealComponent> etherealChildren = null ) {
			Children = children ?? ImmutableArray<FlowComponent>.Empty;
			EtherealChildren = etherealChildren ?? ImmutableArray<EtherealComponent>.Empty;
			LocalDataGetter = localDataGetter;
		}
	}
}