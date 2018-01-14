using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal class IdentifiedEtherealComponent: EtherealComponentOrElement {
		internal readonly Func<IdentifiedComponentData<EtherealComponent>> ComponentDataGetter;

		internal IdentifiedEtherealComponent( Func<IdentifiedComponentData<EtherealComponent>> componentDataGetter ) {
			ComponentDataGetter = componentDataGetter;
		}
	}
}