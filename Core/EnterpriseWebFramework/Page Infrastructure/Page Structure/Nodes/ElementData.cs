using System;
using System.Collections.Generic;

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
		public ElementData( IEnumerable<FlowComponent> children, IEnumerable<EtherealComponent> etherealChildren, Func<ElementLocalData> localDataGetter ) {
			Children = children;
			EtherealChildren = etherealChildren;
			LocalDataGetter = localDataGetter;
		}
	}
}