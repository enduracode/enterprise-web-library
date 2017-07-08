using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An HTML sup element. See https://html.spec.whatwg.org/multipage/text-level-semantics.html#the-sub-and-sup-elements.
	/// </summary>
	public class Superscript: PhrasingComponent {
		private readonly IReadOnlyCollection<DisplayableElement> children;

		/// <summary>
		/// Creates a superscript element.
		/// </summary>
		/// <param name="content"></param>
		/// <param name="displaySetup"></param>
		/// <param name="classes">The classes on the element.</param>
		public Superscript( IEnumerable<PhrasingComponent> content, DisplaySetup displaySetup = null, ElementClassSet classes = null ) {
			children =
				new DisplayableElement(
					context => new DisplayableElementData( displaySetup, () => new DisplayableElementLocalData( "sup" ), classes: classes, children: content ) ).ToCollection();
		}

		IEnumerable<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}
	}
}