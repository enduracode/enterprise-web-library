using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A flow component containing modification error messages.
	/// </summary>
	public class FlowErrorContainer: FlowComponent {
		private readonly IReadOnlyCollection<FlowComponentOrNode> children;

		/// <summary>
		/// Creates a modification-error container with the specified sources.
		/// </summary>
		public FlowErrorContainer( ErrorSourceSet errorSources, ErrorDisplayStyle<FlowComponent> displayStyle, bool disableFocusabilityOnError = false ) {
			children = new IdentifiedFlowComponent(
				() => new IdentifiedComponentData<FlowComponentOrNode>(
					"",
					Enumerable.Empty<UpdateRegionLinker>(),
					errorSources,
					errorsBySource => displayStyle.GetComponents(
						errorSources,
						errorSources.Validations.SelectMany( errorsBySource.GetValidationErrors )
							.Select( i => new TrustedHtmlString( HttpUtility.HtmlEncode( i ) ) )
							.Concat( errorsBySource.GetGeneralErrors() ),
						!disableFocusabilityOnError ) ) ).ToCollection();
		}

		IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}
	}
}