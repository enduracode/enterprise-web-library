using System;
using System.Collections.Generic;
using Humanizer;

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
		/// <param name="etherealChildren"></param>
		public GenericPhrasingContainer(
			IReadOnlyCollection<PhrasingComponent> content, DisplaySetup displaySetup = null, ElementClassSet classes = null,
			IReadOnlyCollection<EtherealComponent> etherealChildren = null ) {
			children = new DisplayableElement(
				context => new DisplayableElementData(
					displaySetup,
					() => new DisplayableElementLocalData( "span" ),
					classes: classes,
					children: content,
					etherealChildren: etherealChildren ) ).ToCollection();
		}

		IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}
	}

	public static class GenericPhrasingContainerExtensionCreators {
		/// <summary>
		/// Creates a generic phrasing container (i.e. span element) that depends on this page-modification value.
		/// </summary>
		public static IReadOnlyCollection<PhrasingComponent> ToGenericPhrasingContainer<ModificationValueType>(
			this PageModificationValue<ModificationValueType> pageModificationValue, Func<ModificationValueType, string> textSelector,
			Func<string, string> jsTextExpressionGetter ) {
			return new CustomPhrasingComponent(
				new DisplayableElement(
					context => {
						pageModificationValue.AddJsModificationStatement(
							valueExpression => "$( '#{0}' ).text( {1} );".FormatWith( context.Id, jsTextExpressionGetter( valueExpression ) ) );
						return new DisplayableElementData(
							null,
							() => new DisplayableElementLocalData( "span", focusDependentData: new DisplayableElementFocusDependentData( includeIdAttribute: true ) ),
							children: new TextNode( () => textSelector( pageModificationValue.Value ) ).ToCollection() );
					} ).ToCollection() ).ToCollection();
		}
	}
}