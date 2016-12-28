using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public interface EtherealComponent: EtherealComponentOrElement {
		IEnumerable<EtherealComponentOrElement> GetChildren();
	}
}