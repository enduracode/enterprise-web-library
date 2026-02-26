using EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure.ElementBase.Classification;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

internal class CssElementCreator: ControlCssElementCreator {
	// This class allows us to use just one selector in the ComponentList elements.
	internal static readonly ElementClass AllListsClass = new( "ewfCl" );

	internal static readonly ElementClass LineListClass = new( "ewfLl" );
	internal static readonly ElementClass StackListClass = new( "ewfSl" );
	internal static readonly ElementClass WrappingListClass = new( "ewfWl" );
	internal static readonly ElementClass InlineListClass = new( "ewfIl" );

	// This class ensures that the item selector does not have lower specificity than the alignment rules in the EWF CSS files.
	internal static readonly ElementClass ItemClass = new( "ewfLi" );

	IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() {
		return new[]
				{
					getListElements( "Unordered", "ul" ), getListElements( "Ordered", "ol" ),
					new CssElement( "ComponentListItem", "li.{0}".FormatWith( ItemClass.ClassName ) ).ToCollection(),
					new CssElement( "InlineListItemContentContainer", "div.{0}".FormatWith( ItemClass.ClassName ) ).ToCollection()
				}.SelectMany( i => i )
			.ToArray();
	}

	private IEnumerable<CssElement> getListElements( string elementNamePrefix, string selectorElement ) {
		yield return new CssElement( elementNamePrefix + "ComponentList", "{0}.{1}".FormatWith( selectorElement, AllListsClass.ClassName ) );
		yield return new CssElement( elementNamePrefix + "LineList", "{0}.{1}".FormatWith( selectorElement, LineListClass.ClassName ) );
		yield return new CssElement( elementNamePrefix + "StackList", "{0}.{1}".FormatWith( selectorElement, StackListClass.ClassName ) );
		yield return new CssElement( elementNamePrefix + "WrappingList", "{0}.{1}".FormatWith( selectorElement, WrappingListClass.ClassName ) );
		yield return new CssElement( elementNamePrefix + "InlineList", "{0}.{1}".FormatWith( selectorElement, InlineListClass.ClassName ) );
	}
}