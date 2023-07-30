#nullable disable
using System.Collections.Generic;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A flow component that prevents its children from affecting the ID of any other component.
	/// </summary>
	public class FlowIdContainer: FlowComponent {
		private readonly FlowComponentOrNode identifiedComponent;

		/// <summary>
		/// Creates an ID container.
		/// </summary>
		/// <param name="children"></param>
		/// <param name="updateRegionSets">The intermediate-post-back update-region sets that this component will be a part of.</param>
		public FlowIdContainer( IEnumerable<FlowComponent> children, IEnumerable<UpdateRegionSet> updateRegionSets = null ) {
			identifiedComponent = new IdentifiedFlowComponent(
				() => new IdentifiedComponentData<FlowComponentOrNode>(
					"",
					new UpdateRegionLinker(
						"",
						new PreModificationUpdateRegion( updateRegionSets, identifiedComponent.ToCollection, () => "" ).ToCollection(),
						arg => identifiedComponent.ToCollection() ).ToCollection(),
					new ErrorSourceSet(),
					errorsBySource => children ) );
		}

		IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() => identifiedComponent.ToCollection();
	}
}