using System;
using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Data for an element.
	/// </summary>
	public class ElementData {
		internal readonly Func<ElementContext, ElementNodeData> NodeDataGetter;

		/// <summary>
		/// Creates an element-data object.
		/// </summary>
		public ElementData(
			Func<ElementLocalData> localDataGetter, ElementClassSet classes = null, IEnumerable<FlowComponentOrNode> children = null,
			IEnumerable<EtherealComponentOrElement> etherealChildren = null ) {
			classes = classes ?? ElementClassSet.Empty;
			NodeDataGetter = context => {
				classes.AddElementId( context.Id );
				return new ElementNodeData( () => localDataGetter().NodeDataGetter( classes ), children: children, etherealChildren: etherealChildren );
			};
		}
	}
}