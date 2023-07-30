#nullable disable
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

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
	/// Includes the current page, but uses the entity-setup name if an entity setup exists and is also the parent.
	/// </summary>
	IncludeCurrentPageAndUseEntitySetupNameIfEntitySetupIsParent
}

/// <summary>
/// A component that displays the full path to the current page, optionally including the page’s name as a first-level heading.
/// </summary>
public class PagePath: FlowComponent {
	private static readonly ElementClass elementClass = new( "ewfPagePath" );

	[ UsedImplicitly ]
	private class CssElementCreator: ControlCssElementCreator {
		IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() =>
			new CssElement( "PagePathContainer", "div.{0}".FormatWith( elementClass.ClassName ) ).ToCollection();
	}

	private readonly IReadOnlyCollection<WrappingListItem> hyperlinkItems;
	private readonly PageName pageName;
	private readonly IReadOnlyCollection<FlowComponent> children;

	/// <summary>
	/// Creates a page-path component.
	/// </summary>
	public PagePath( PagePathCurrentPageBehavior currentPageBehavior = PagePathCurrentPageBehavior.IncludeCurrentPage ) {
		var ancestors = PageBase.Current.Ancestors;
		if( currentPageBehavior == PagePathCurrentPageBehavior.IncludeCurrentPageAndUseEntitySetupNameIfEntitySetupIsParent &&
		    PageBase.Current.EntitySetupIsParent )
			ancestors = ancestors.Skip( 1 );

		hyperlinkItems = ancestors.Reverse()
			.Select(
				ancestor => (WrappingListItem)new EwfHyperlink(
						ancestor is EntitySetupBase es ? es.DefaultResource : (ResourceBase)ancestor,
						ancestor.Name.Length > 0 ? new StandardHyperlinkStyle( ancestor.Name ) : new ImageHyperlinkStyle( new StaticFiles.Ui.Home_iconSvg(), "Home" ) )
					.ToComponentListItem() )
			.Materialize();

		if( currentPageBehavior != PagePathCurrentPageBehavior.ExcludeCurrentPage )
			pageName = new PageName(
				useEntitySetupNameIfEntitySetupIsParent:
				currentPageBehavior == PagePathCurrentPageBehavior.IncludeCurrentPageAndUseEntitySetupNameIfEntitySetupIsParent );

		children = new GenericFlowContainer(
			( hyperlinkItems.Any() ? new WrappingList( hyperlinkItems ).ToCollection() : Enumerable.Empty<FlowComponent>() )
			.Concat( pageName is not null ? pageName.ToCollection() : Enumerable.Empty<FlowComponent>() )
			.Materialize(),
			classes: elementClass ).ToCollection();
	}

	/// <summary>
	/// Returns true if this component will not display any content.
	/// </summary>
	public bool IsEmpty => !hyperlinkItems.Any() && ( pageName is null || pageName.IsEmpty );

	IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() => children;
}