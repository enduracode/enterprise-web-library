using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An HTML span element.
	/// </summary>
	public class GenericPhrasingContainer: PhrasingComponent {
		private readonly IReadOnlyCollection<DisplayableElement> children;

		/// <summary>
		/// Creates a generic phrasing container (i.e. span element).
		/// </summary>
		/// <param name="content"></param>
		/// <param name="displaySetup"></param>
		/// <param name="classes">The classes on the element.</param>
		public GenericPhrasingContainer( IEnumerable<PhrasingComponent> content, DisplaySetup displaySetup = null, ElementClassSet classes = null ) {
			children =
				new DisplayableElement(
					context => new DisplayableElementData( displaySetup, () => new DisplayableElementLocalData( "span" ), classes: classes, children: content ) ).ToCollection(
						);
		}

		IEnumerable<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}
	}
}