using System.Collections.Generic;
using System.Linq;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A first-level heading that displays the page’s name.
	/// </summary>
	public class PageName: FlowComponent {
		private readonly string pageName;
		private readonly IReadOnlyCollection<DisplayableElement> children;

		/// <summary>
		/// Creates a page-name heading.
		/// </summary>
		/// <param name="excludePageNameIfEntitySetupExists">Pass true to exclude the page name if an entity setup exists.</param>
		public PageName( bool excludePageNameIfEntitySetupExists = false ) {
			var es = EwfPage.Instance.EsAsBaseType;
			var info = EwfPage.Instance.InfoAsBaseType;
			pageName = excludePageNameIfEntitySetupExists && es != null && info.ParentResource == null ? es.EntitySetupName : info.ResourceFullName;

			children = new DisplayableElement(
				context => new DisplayableElementData( null, () => new DisplayableElementLocalData( "h1" ), children: pageName.ToComponents() ) ).ToCollection();
		}

		/// <summary>
		/// Returns true if this component will not display any content.
		/// </summary>
		public bool IsEmpty => !pageName.Any();

		IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() => children;
	}
}