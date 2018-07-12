using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An HTML em element. See https://html.spec.whatwg.org/multipage/text-level-semantics.html#the-em-element.
	/// </summary>
	public class EmphasizedContent: PhrasingComponent {
		private readonly IReadOnlyCollection<DisplayableElement> children;

		/// <summary>
		/// Creates an emphasized-content (i.e. em) element.
		/// </summary>
		/// <param name="content"></param>
		/// <param name="displaySetup"></param>
		/// <param name="classes">The classes on the element.</param>
		public EmphasizedContent( IReadOnlyCollection<PhrasingComponent> content, DisplaySetup displaySetup = null, ElementClassSet classes = null ) {
			children = new DisplayableElement(
					context => new DisplayableElementData( displaySetup, () => new DisplayableElementLocalData( "em" ), classes: classes, children: content ) )
				.ToCollection();
		}

		IEnumerable<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}
	}
}