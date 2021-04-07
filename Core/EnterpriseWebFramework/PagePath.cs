using System.Collections.Generic;
using System.Linq;
using Humanizer;
using JetBrains.Annotations;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A current page behavior for a page path.
	/// </summary>
	public enum PagePathCurrentPageBehavior {
		/// <summary>
		/// Excludes the current page.
		/// </summary>
		ExcludeCurrentPage,

		/// <summary>
		/// Includes the current page.
		/// </summary>
		IncludeCurrentPage,

		/// <summary>
		/// Includes the current page, but excludes the page name if an entity setup exists.
		/// </summary>
		IncludeCurrentPageAndExcludePageNameIfEntitySetupExists
	}

	/// <summary>
	/// A component that displays the full path to the current page, optionally including the page’s name as a first-level heading.
	/// </summary>
	public class PagePath: FlowComponent {
		private static readonly ElementClass elementClass = new ElementClass( "ewfPagePath" );

		[ UsedImplicitly ]
		private class CssElementCreator: ControlCssElementCreator {
			IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() =>
				new CssElement( "PagePath", "div.{0}".FormatWith( elementClass.ClassName ) ).ToCollection();
		}

		private readonly PageName pageName;
		private readonly IReadOnlyCollection<FlowComponent> children;

		/// <summary>
		/// Creates a page-path component.
		/// </summary>
		public PagePath( PagePathCurrentPageBehavior currentPageBehavior = PagePathCurrentPageBehavior.IncludeCurrentPage ) {
			if( currentPageBehavior != PagePathCurrentPageBehavior.ExcludeCurrentPage )
				pageName = new PageName(
					excludePageNameIfEntitySetupExists: currentPageBehavior == PagePathCurrentPageBehavior.IncludeCurrentPageAndExcludePageNameIfEntitySetupExists );

			var pagePath = PageBase.Current.ResourcePath;
			var components = new List<FlowComponent>();
			foreach( var resource in pagePath.Take( pagePath.Count - 1 ) ) {
				components.Add( new EwfHyperlink( resource, new StandardHyperlinkStyle( resource.ResourceFullName ) ) );
				components.AddRange( ResourceBase.ResourcePathSeparator.ToComponents() );
			}
			if( pageName != null )
				components.Add( pageName );
			else if( components.Any() )
				components.RemoveAt( components.Count - 1 );
			children = new GenericFlowContainer( components, classes: elementClass ).ToCollection();
		}

		/// <summary>
		/// Returns true if this component will not display any content.
		/// </summary>
		public bool IsEmpty => PageBase.Current.ResourcePath.Count == 1 && ( pageName == null || pageName.IsEmpty );

		IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() => children;
	}
}