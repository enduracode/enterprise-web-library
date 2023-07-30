#nullable disable
using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public interface FlowComponent: FlowComponentOrNode {
		IReadOnlyCollection<FlowComponentOrNode> GetChildren();
	}
}