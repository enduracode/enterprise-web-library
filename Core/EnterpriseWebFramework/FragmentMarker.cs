using System.Collections.Generic;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An invisible marker used in conjunction with fragment navigation.
	/// </summary>
	public sealed class FragmentMarker: PhrasingComponent {
		private readonly IReadOnlyCollection<FlowComponent> children;

		/// <summary>
		/// Creates a fragment marker.
		/// </summary>
		/// <param name="id">The ID of the marker, which should be unique on the page. Do not pass null or the empty string.</param>
		public FragmentMarker( string id ) {
			children = new ElementComponent(
				context => new ElementData(
					() => new ElementLocalData( "span", focusDependentData: new ElementFocusDependentData( includeIdAttribute: true ) ),
					clientSideIdOverride: id ) ).ToCollection();
		}

		IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() => children;
	}
}