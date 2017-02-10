using System;
using System.Collections.Generic;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A displayable element.
	/// </summary>
	public class DisplayableElement: FlowComponent, EtherealComponent {
		private readonly PageElement element;

		/// <summary>
		/// Creates a displayable element.
		/// </summary>
		public DisplayableElement( Func<ElementContext, DisplayableElementData> elementDataGetter, FormValue formValue = null ) {
			element = new PageElement(
				context => {
					var data = elementDataGetter( context );

					data.DisplaySetup.AddJsShowStatements( "$( '#{0}' ).show( 200 );".FormatWith( context.Id ) );
					data.DisplaySetup.AddJsHideStatements( "$( '#{0}' ).hide( 200 );".FormatWith( context.Id ) );

					return new ElementData(
						() => {
							var localData = data.LocalDataGetter();

							var attributes = new List<Tuple<string, string>>();
							attributes.AddRange( localData.Attributes );
							if( !data.DisplaySetup.ComponentsDisplayed )
								attributes.Add( Tuple.Create( "style", "display: none" ) );

							return new ElementLocalData(
								localData.ElementName,
								attributes,
								data.DisplaySetup.UsesJsStatements || localData.IncludeIdAttribute,
								localData.JsInitStatements );
						},
						children: data.Children,
						etherealChildren: data.EtherealChildren );
				},
				formValue: formValue );
		}

		IEnumerable<FlowComponentOrNode> FlowComponent.GetChildren() {
			return element.ToCollection();
		}

		IEnumerable<EtherealComponentOrElement> EtherealComponent.GetChildren() {
			return element.ToCollection();
		}
	}
}