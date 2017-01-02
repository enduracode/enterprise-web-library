using System.Collections.Generic;
using System.Collections.Immutable;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A phrasing component that prevents its children from affecting the ID of any other component.
	/// </summary>
	public class PhrasingIdContainer: PhrasingComponent {
		private readonly IReadOnlyCollection<FlowComponentOrNode> children;

		/// <summary>
		/// Creates an ID container.
		/// </summary>
		public PhrasingIdContainer( IEnumerable<PhrasingComponent> children ) {
			this.children =
				new IdentifiedFlowComponent(
					() =>
					new IdentifiedComponentData<FlowComponentOrNode>(
						true,
						ImmutableArray<UpdateRegionLinker>.Empty,
						ImmutableArray<EwfValidation>.Empty,
						errorsByValidation => children ) ).ToCollection();
		}

		IEnumerable<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}
	}
}