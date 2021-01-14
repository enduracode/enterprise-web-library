using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;
using JetBrains.Annotations;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A style that displays errors as a list.
	/// </summary>
	public class ListErrorDisplayStyle: ErrorDisplayStyle<FlowComponent> {
		private static readonly ElementClass containerClass = new ElementClass( "ewfEml" );

		/// <summary>
		/// EWL use only.
		/// </summary>
		public static readonly IReadOnlyCollection<string> CssSelectors = "div.{0}".FormatWith( containerClass.ClassName ).ToCollection();

		[ UsedImplicitly ]
		private class CssElementCreator: ControlCssElementCreator {
			IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() {
				return new CssElement( "ErrorMessageListContainer", CssSelectors.ToArray() ).ToCollection();
			}
		}

		internal static DisplayableElementLocalData GetErrorFocusableElementLocalData(
			ElementContext context, string elementName, ErrorSourceSet errorSources, IReadOnlyCollection<ElementAttribute> attributes ) =>
			new DisplayableElementLocalData(
				elementName,
				new FocusabilityCondition( false, errorFocusabilitySources: errorSources ),
				isFocused => {
					if( isFocused )
						attributes = ( attributes ?? Enumerable.Empty<ElementAttribute>() ).Append( new ElementAttribute( "tabindex", "-1" ) ).Materialize();
					return new DisplayableElementFocusDependentData(
						attributes: attributes,
						includeIdAttribute: isFocused,
						jsInitStatements: isFocused
							                  ? "document.getElementById( '{0}' ).focus(); document.getElementById( '{0}' ).scrollIntoView( {{ behavior: 'smooth', block: 'start' }} );"
								                  .FormatWith( context.Id )
							                  : "" );
				} );

		private readonly Func<ErrorSourceSet, IEnumerable<TrustedHtmlString>, bool, IReadOnlyCollection<FlowComponent>> componentGetter;

		/// <summary>
		/// Creates a list error-display style.
		/// </summary>
		/// <param name="classes">The classes on the list container.</param>
		public ListErrorDisplayStyle( ElementClassSet classes = null ) {
			componentGetter = ( errorSources, errors, componentsFocusableOnError ) => {
				if( !errors.Any() )
					return Enumerable.Empty<FlowComponent>().Materialize();

				return new DisplayableElement(
					context => new DisplayableElementData(
						null,
						() => GetErrorFocusableElementLocalData( context, "div", componentsFocusableOnError ? errorSources : null, null ),
						classes: containerClass.Add( classes ?? ElementClassSet.Empty ),
						children: new StackList(
							from i in errors
							select new FontAwesomeIcon( "fa-times-circle", "fa-lg" ).ToCollection<PhrasingComponent>()
								.Concat( " ".ToComponents() )
								.Append( i.ToComponent() )
								.Materialize()
								.ToComponentListItem() ).ToCollection() ) ).ToCollection();
			};
		}

		IReadOnlyCollection<FlowComponent> ErrorDisplayStyle<FlowComponent>.GetComponents(
			ErrorSourceSet errorSources, IEnumerable<TrustedHtmlString> errors, bool componentsFocusableOnError ) =>
			componentGetter( errorSources, errors, componentsFocusableOnError );
	}
}