using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An HTML sub element. See https://html.spec.whatwg.org/multipage/text-level-semantics.html#the-sub-and-sup-elements.
	/// </summary>
	public class Subscript: PhrasingComponent {
		private readonly IReadOnlyCollection<DisplayableElement> children;

		/// <summary>
		/// Creates a subscript element.
		/// </summary>
		/// <param name="content"></param>
		/// <param name="displaySetup"></param>
		/// <param name="classes">The classes on the element.</param>
		public Subscript( IReadOnlyCollection<PhrasingComponent> content, DisplaySetup displaySetup = null, ElementClassSet classes = null ) {
			children = new DisplayableElement(
					context => new DisplayableElementData( displaySetup, () => new DisplayableElementLocalData( "sub" ), classes: classes, children: content ) )
				.ToCollection();
		}

		IEnumerable<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}
	}
}