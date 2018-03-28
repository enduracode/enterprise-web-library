using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A phrasing component that prevents its children from affecting the ID of any other component.
	/// </summary>
	public class PhrasingIdContainer: PhrasingComponent {
		private readonly IReadOnlyCollection<FlowComponentOrNode> children;

		/// <summary>
		/// Creates an ID container.
		/// </summary>
		/// <param name="children"></param>
		/// <param name="updateRegionSets">The intermediate-post-back update-region sets that this component will be a part of.</param>
		public PhrasingIdContainer( IEnumerable<PhrasingComponent> children, IEnumerable<UpdateRegionSet> updateRegionSets = null ) {
			this.children = new IdentifiedFlowComponent(
				() => new IdentifiedComponentData<FlowComponentOrNode>(
					"",
					new UpdateRegionLinker( "", new PreModificationUpdateRegion( updateRegionSets, this.ToCollection, () => "" ).ToCollection(), arg => this.ToCollection() )
						.ToCollection(),
					new ErrorSourceSet(),
					errorsBySource => children ) ).ToCollection();
		}

		IEnumerable<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}
	}
}