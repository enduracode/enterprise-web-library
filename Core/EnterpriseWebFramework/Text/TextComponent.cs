using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Text for a page.
	/// </summary>
	public sealed class TextComponent: PhrasingComponent {
		private readonly IReadOnlyCollection<FlowComponentOrNode> children;

		internal TextComponent( string text ) {
			children = new TextNode( () => text ).ToCollection();
		}

		IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}
	}

	public static class TextComponentExtensionCreators {
		/// <summary>
		/// Creates a collection of components representing this string.
		/// </summary>
		/// <param name="s">Do not pass null.</param>
		/// <param name="disableNewlineReplacement">Pass true if you want newlines passed through to the HTML source rather than being replaced with
		/// <see cref="LineBreak"/> components.</param>
		public static IReadOnlyCollection<PhrasingComponent> ToComponents( this string s, bool disableNewlineReplacement = false ) {
			if( disableNewlineReplacement )
				return new TextComponent( s ).ToCollection();

			return
				s.Separate( Environment.NewLine, false )
					.Aggregate(
						(IEnumerable<PhrasingComponent>)null,
						( collection, line ) =>
						collection?.Concat( new PhrasingComponent[] { new LineBreak(), new TextComponent( line ) } ) ?? new TextComponent( line ).ToCollection() )
					.ToImmutableArray();
		}
	}
}