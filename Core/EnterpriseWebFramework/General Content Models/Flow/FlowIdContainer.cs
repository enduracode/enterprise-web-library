using System.Collections.Generic;
using System.Collections.Immutable;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A flow component that prevents its children from affecting the ID of any other component.
	/// </summary>
	public class FlowIdContainer: FlowComponent {
		private readonly IReadOnlyCollection<FlowComponentOrNode> children;

		/// <summary>
		/// Creates an ID container.
		/// </summary>
		public FlowIdContainer( IEnumerable<FlowComponentOrNode> children ) {
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