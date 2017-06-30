using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An HTML strong element. See https://html.spec.whatwg.org/multipage/text-level-semantics.html#the-strong-element.
	/// </summary>
	public class ImportantContent: PhrasingComponent {
		private readonly IReadOnlyCollection<DisplayableElement> children;

		/// <summary>
		/// Creates an important-content (i.e. strong) element.
		/// </summary>
		/// <param name="content"></param>
		/// <param name="displaySetup"></param>
		/// <param name="classes">The classes on the element.</param>
		public ImportantContent( IEnumerable<PhrasingComponent> content, DisplaySetup displaySetup = null, ElementClassSet classes = null ) {
			children =
				new DisplayableElement(
					context => new DisplayableElementData( displaySetup, () => new DisplayableElementLocalData( "strong" ), classes: classes, children: content ) )
					.ToCollection();
		}

		IEnumerable<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}
	}
}