using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A block of markup.
	/// </summary>
	public sealed class MarkupBlockComponent: FlowComponent {
		private readonly IReadOnlyCollection<FlowComponentOrNode> children;

		internal MarkupBlockComponent( string markup ) {
			children = new MarkupBlockNode( () => markup ).ToCollection();
		}

		IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}
	}

	public static class MarkupBlockComponentExtensionCreators {
		/// <summary>
		/// Creates a component representing this HTML string.
		/// </summary>
		/// <param name="s">Do not pass null.</param>
		public static FlowComponent ToComponent( this TrustedHtmlString s ) {
			return new MarkupBlockComponent( s.Html );
		}
	}
}