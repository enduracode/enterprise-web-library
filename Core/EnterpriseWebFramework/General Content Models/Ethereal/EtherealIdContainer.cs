using System.Collections.Generic;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An ethereal component that prevents its children from affecting the ID of any other component.
	/// </summary>
	public class EtherealIdContainer: EtherealComponent {
		private readonly EtherealComponentOrElement identifiedComponent;

		/// <summary>
		/// Creates an ID container.
		/// </summary>
		/// <param name="children"></param>
		/// <param name="updateRegionSets">The intermediate-post-back update-region sets that this component will be a part of.</param>
		public EtherealIdContainer( IEnumerable<EtherealComponent> children, IEnumerable<UpdateRegionSet> updateRegionSets = null ) {
			identifiedComponent = new IdentifiedEtherealComponent(
				() => new IdentifiedComponentData<EtherealComponentOrElement>(
					"",
					new UpdateRegionLinker(
						"",
						new PreModificationUpdateRegion( updateRegionSets, identifiedComponent.ToCollection, () => "" ).ToCollection(),
						arg => identifiedComponent.ToCollection() ).ToCollection(),
					new ErrorSourceSet(),
					errorsBySource => children ) );
		}

		IReadOnlyCollection<EtherealComponentOrElement> EtherealComponent.GetChildren() => identifiedComponent.ToCollection();
	}
}