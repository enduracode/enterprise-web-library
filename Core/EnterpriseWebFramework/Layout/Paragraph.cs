using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A paragraph. See https://html.spec.whatwg.org/multipage/dom.html#paragraph.
	/// </summary>
	public class Paragraph: FlowComponent {
		private readonly IReadOnlyCollection<DisplayableElement> children;

		/// <summary>
		/// Creates a paragraph.
		/// </summary>
		/// <param name="content"></param>
		/// <param name="displaySetup"></param>
		/// <param name="classes">The classes on the element.</param>
		public Paragraph( IEnumerable<PhrasingComponent> content, DisplaySetup displaySetup = null, ElementClassSet classes = null ) {
			children =
				new DisplayableElement(
					context => new DisplayableElementData( displaySetup, () => new DisplayableElementLocalData( "p" ), classes: classes, children: content ) ).ToCollection();
		}

		IEnumerable<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}
	}
}