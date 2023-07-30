#nullable disable
using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public interface EtherealComponent: EtherealComponentOrElement {
		IReadOnlyCollection<EtherealComponentOrElement> GetChildren();
	}
}