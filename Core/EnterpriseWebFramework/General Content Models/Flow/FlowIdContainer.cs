using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A flow component that prevents its children from affecting the ID of any other component.
	/// </summary>
	public class FlowIdContainer: FlowComponent {
		private readonly IReadOnlyCollection<FlowComponentOrNode> children;

		/// <summary>
		/// Creates an ID container.
		/// </summary>
		/// <param name="children"></param>
		/// <param name="updateRegionSets">The intermediate-post-back update-region sets that this component will be a part of.</param>
		public FlowIdContainer( IEnumerable<FlowComponent> children, IEnumerable<UpdateRegionSet> updateRegionSets = null ) {
			this.children = new IdentifiedFlowComponent(
				() => new IdentifiedComponentData<FlowComponentOrNode>(
					"",
					new UpdateRegionLinker( "", new PreModificationUpdateRegion( updateRegionSets, this.ToCollection, () => "" ).ToCollection(), arg => this.ToCollection() )
						.ToCollection(),
					new ErrorSourceSet(),
					errorsBySource => children ) ).ToCollection();
		}

		IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}
	}
}