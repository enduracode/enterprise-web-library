using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal class IdentifiedEtherealComponent: EtherealComponentOrElement {
		internal readonly Func<IdentifiedComponentData<EtherealComponentOrElement>> ComponentDataGetter;

		internal IdentifiedEtherealComponent( Func<IdentifiedComponentData<EtherealComponentOrElement>> componentDataGetter ) {
			ComponentDataGetter = componentDataGetter;
		}
	}
}