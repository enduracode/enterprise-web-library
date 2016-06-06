using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public interface FlowComponent: PageComponent {
		IEnumerable<PageNode> GetNodes();
	}
}