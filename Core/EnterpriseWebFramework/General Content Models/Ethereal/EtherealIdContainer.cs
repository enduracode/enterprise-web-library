using System.Collections.Generic;
using System.Collections.Immutable;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An ethereal component that prevents its children from affecting the ID of any other component.
	/// </summary>
	public class EtherealIdContainer: EtherealComponent {
		private readonly IReadOnlyCollection<EtherealComponentOrElement> children;

		/// <summary>
		/// Creates an ID container.
		/// </summary>
		public EtherealIdContainer( IEnumerable<EtherealComponentOrElement> children ) {
			this.children =
				new IdentifiedEtherealComponent(
					() =>
					new IdentifiedComponentData<EtherealComponentOrElement>(
						true,
						ImmutableArray<UpdateRegionLinker>.Empty,
						ImmutableArray<EwfValidation>.Empty,
						errorsByValidation => children ) ).ToCollection();
		}

		IEnumerable<EtherealComponentOrElement> EtherealComponent.GetChildren() {
			return children;
		}
	}
}