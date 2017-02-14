using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Text for a page.
	/// </summary>
	public sealed class TextComponent: PhrasingComponent {
		private readonly IReadOnlyCollection<FlowComponentOrNode> children;

		internal TextComponent( string text ) {
			children = new TextNode( () => text ).ToCollection();
		}

		IEnumerable<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}
	}

	public static class EwfTextExtensionCreators {
		/// <summary>
		/// Creates a text component containing this string.
		/// </summary>
		/// <param name="s">Do not pass null.</param>
		public static TextComponent ToComponent( this string s ) {
			return new TextComponent( s );
		}
	}
}