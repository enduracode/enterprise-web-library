using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public interface EtherealComponent: PageComponent {
		IEnumerable<EtherealElement> GetElements();
	}
}