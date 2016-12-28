using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal class IdentifiedFlowComponent: FlowComponent {
		internal readonly IEnumerable<UpdateRegionLinker> UpdateRegionLinkers;
		private readonly IEnumerable<FlowComponentOrNode> children;

		internal IdentifiedFlowComponent( IEnumerable<UpdateRegionLinker> updateRegionLinkers, IEnumerable<FlowComponentOrNode> children ) {
			UpdateRegionLinkers = updateRegionLinkers;
			this.children = children;
		}

		IEnumerable<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}
	}
}