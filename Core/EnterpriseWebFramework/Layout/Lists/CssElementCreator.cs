using System.Collections.Generic;
using System.Linq;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal class CssElementCreator: ControlCssElementCreator {
		// This class allows us to use just one selector in the ComponentList elements.
		internal static readonly ElementClass AllListsClass = new ElementClass( "ewfCl" );

		internal static readonly ElementClass LineListClass = new ElementClass( "ewfLl" );
		internal static readonly ElementClass StackListClass = new ElementClass( "ewfSl" );
		internal static readonly ElementClass WrappingListClass = new ElementClass( "ewfWl" );

		// This class allows the item selector to have the same specificity as the alignment rules in the EWF CSS files.
		internal static readonly ElementClass AllItemAlignmentsClass = new ElementClass( "ewfLi" );

		CssElement[] ControlCssElementCreator.CreateCssElements() {
			return
				new[]
					{
						getListElements( "Unordered", "ul" ), getListElements( "Ordered", "ol" ),
						new CssElement( "ComponentListItem", "li.{0}".FormatWith( AllItemAlignmentsClass.ClassName ) ).ToCollection()
					}.SelectMany( i => i ).ToArray();
		}

		private IEnumerable<CssElement> getListElements( string elementNamePrefix, string selectorElement ) {
			yield return new CssElement( elementNamePrefix + "ComponentList", "{0}.{1}".FormatWith( selectorElement, AllListsClass.ClassName ) );
			yield return new CssElement( elementNamePrefix + "LineList", "{0}.{1}".FormatWith( selectorElement, LineListClass.ClassName ) );
			yield return new CssElement( elementNamePrefix + "StackList", "{0}.{1}".FormatWith( selectorElement, StackListClass.ClassName ) );
			yield return new CssElement( elementNamePrefix + "WrappingList", "{0}.{1}".FormatWith( selectorElement, WrappingListClass.ClassName ) );
		}
	}
}