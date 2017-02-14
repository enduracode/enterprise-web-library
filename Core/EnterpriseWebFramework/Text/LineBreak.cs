using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A line break.
	/// </summary>
	public sealed class LineBreak: PhrasingComponent {
		private readonly IReadOnlyCollection<FlowComponentOrNode> children;

		/// <summary>
		/// Creates a line break.
		/// </summary>
		public LineBreak() {
			children = new ElementComponent( context => new ElementData( () => new ElementLocalData( "br" ) ) ).ToCollection();
		}

		IEnumerable<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}
	}
}