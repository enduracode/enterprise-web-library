using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An HTML small element. See https://html.spec.whatwg.org/multipage/semantics.html#the-small-element.
	/// </summary>
	public class SideComments: FlowComponent {
		private readonly IReadOnlyCollection<DisplayableElement> children;

		/// <summary>
		/// Creates a side-comments (i.e. small) element.
		/// </summary>
		/// <param name="content"></param>
		/// <param name="displaySetup"></param>
		/// <param name="classes">The classes on the element.</param>
		public SideComments( IEnumerable<PhrasingComponent> content, DisplaySetup displaySetup = null, ElementClassSet classes = null ) {
			children =
				new DisplayableElement(
					context => new DisplayableElementData( displaySetup, () => new DisplayableElementLocalData( "small" ), classes: classes, children: content ) ).ToCollection
					();
		}

		IEnumerable<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}
	}
}