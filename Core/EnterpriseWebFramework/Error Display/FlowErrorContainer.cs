using System.Collections.Generic;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A flow component containing modification error messages.
	/// </summary>
	public class FlowErrorContainer: FlowComponent {
		private readonly IReadOnlyCollection<FlowComponentOrNode> children;

		/// <summary>
		/// Creates a modification-error container with the specified sources.
		/// </summary>
		public FlowErrorContainer( ErrorSourceSet errorSources, ErrorDisplayStyle<FlowComponent> displayStyle ) {
			children = new IdentifiedFlowComponent(
				() => new IdentifiedComponentData<FlowComponentOrNode>(
					"",
					Enumerable.Empty<UpdateRegionLinker>(),
					errorSources,
					errorsBySource => displayStyle.GetComponents(
						errorSources,
						errorSources.Validations.SelectMany( errorsBySource.GetValidationErrors ).Concat( errorsBySource.GetGeneralErrors() ),
						true ) ) ).ToCollection();
		}

		IEnumerable<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}
	}
}