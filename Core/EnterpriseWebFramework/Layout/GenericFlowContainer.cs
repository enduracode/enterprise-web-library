using System.Collections.Generic;
using System.Linq;
using Tewl.Tools;

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
		/// <param name="clientSideIdOverride">Pass a nonempty string to override the client-side ID of the element, which is useful if you need a static value that
		/// you can reference from CSS or JavaScript files. The ID you specify should be unique on the page. Do not pass null. Use with caution.</param>
		/// <param name="etherealContent"></param>
		public GenericFlowContainer(
			IReadOnlyCollection<FlowComponent> content, DisplaySetup displaySetup = null, ElementClassSet classes = null, string clientSideIdOverride = "",
			IReadOnlyCollection<EtherealComponent> etherealContent = null ) {
			children = new DisplayableElement(
				context => new DisplayableElementData(
					displaySetup,
					() => new DisplayableElementLocalData(
						"div",
						focusDependentData: new DisplayableElementFocusDependentData( includeIdAttribute: clientSideIdOverride.Any() ) ),
					classes: classes,
					clientSideIdOverride: clientSideIdOverride,
					children: content,
					etherealChildren: etherealContent ) ).ToCollection();
		}

		IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() => children;
	}
}