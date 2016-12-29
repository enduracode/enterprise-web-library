using System;
using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Data for a displayable element.
	/// </summary>
	public class DisplayableElementData {
		internal readonly DisplaySetup DisplaySetup;
		internal readonly IEnumerable<FlowComponentOrNode> Children;
		internal readonly IEnumerable<EtherealComponentOrElement> EtherealChildren;
		internal readonly Func<DisplayableElementLocalData> LocalDataGetter;

		/// <summary>
		/// Creates a displayable-element-data object.
		/// </summary>
		public DisplayableElementData(
			DisplaySetup displaySetup, Func<DisplayableElementLocalData> localDataGetter, IEnumerable<FlowComponentOrNode> children = null,
			IEnumerable<EtherealComponentOrElement> etherealChildren = null ) {
			DisplaySetup = displaySetup ?? new DisplaySetup( true );
			Children = children;
			EtherealChildren = etherealChildren;
			LocalDataGetter = localDataGetter;
		}
	}
}