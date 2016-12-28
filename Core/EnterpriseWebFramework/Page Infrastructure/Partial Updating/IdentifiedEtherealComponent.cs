using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal class IdentifiedEtherealComponent: EtherealComponent {
		internal readonly IEnumerable<UpdateRegionLinker> UpdateRegionLinkers;
		private readonly IEnumerable<EtherealComponentOrElement> children;

		internal IdentifiedEtherealComponent( IEnumerable<UpdateRegionLinker> updateRegionLinkers, IEnumerable<EtherealComponentOrElement> children ) {
			UpdateRegionLinkers = updateRegionLinkers;
			this.children = children;
		}

		IEnumerable<EtherealComponentOrElement> EtherealComponent.GetChildren() {
			return children;
		}
	}
}