#nullable disable
using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal class IdentifiedFlowComponent: FlowComponentOrNode {
		internal readonly Func<IdentifiedComponentData<FlowComponentOrNode>> ComponentDataGetter;

		internal IdentifiedFlowComponent( Func<IdentifiedComponentData<FlowComponentOrNode>> componentDataGetter ) {
			ComponentDataGetter = componentDataGetter;
		}
	}
}