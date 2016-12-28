using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public interface FlowComponent: FlowComponentOrNode {
		IEnumerable<FlowComponentOrNode> GetChildren();
	}
}