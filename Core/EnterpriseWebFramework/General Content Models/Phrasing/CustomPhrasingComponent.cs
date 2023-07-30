#nullable disable
using System.Collections.Generic;
using System.Collections.Immutable;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A phrasing component with custom children.
	/// </summary>
	public class CustomPhrasingComponent: PhrasingComponent {
		private readonly IReadOnlyCollection<FlowComponentOrNode> children;

		/// <summary>
		/// Creates a custom phrasing component.
		/// </summary>
		public CustomPhrasingComponent( IEnumerable<FlowComponent> children ) {
			this.children = children.ToImmutableArray();
		}

		IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() => children;
	}
}