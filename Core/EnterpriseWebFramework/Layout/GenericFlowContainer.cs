using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An HTML div element.
	/// </summary>
	public class GenericFlowContainer: FlowComponent {
		private readonly IReadOnlyCollection<DisplayableElement> children;

		/// <summary>
		/// Creates a generic flow container (i.e. div element).
		/// </summary>
		/// <param name="content"></param>
		/// <param name="displaySetup"></param>
		/// <param name="classes">The classes on the element.</param>
		/// <param name="etherealContent"></param>
		public GenericFlowContainer(
			IReadOnlyCollection<FlowComponent> content, DisplaySetup displaySetup = null, ElementClassSet classes = null,
			IReadOnlyCollection<EtherealComponent> etherealContent = null ) {
			children = new DisplayableElement(
				context => new DisplayableElementData(
					displaySetup,
					() => new DisplayableElementLocalData( "div" ),
					classes: classes,
					children: content,
					etherealChildren: etherealContent ) ).ToCollection();
		}

		IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}
	}
}