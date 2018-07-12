using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Humanizer;
using JetBrains.Annotations;

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

		private readonly Func<ErrorSourceSet, IEnumerable<TrustedHtmlString>, bool, IReadOnlyCollection<FlowComponent>> componentGetter;

		/// <summary>
		/// Creates a list error-display style.
		/// </summary>
		/// <param name="classes">The classes on the list container.</param>
		public ListErrorDisplayStyle( ElementClassSet classes = null ) {
			componentGetter = ( errorSources, errors, componentsFocusableOnError ) => {
				if( !errors.Any() )
					return ImmutableArray<FlowComponent>.Empty;

				return new DisplayableElement(
					context => new DisplayableElementData(
						null,
						() => new DisplayableElementLocalData(
							"div",
							new FocusabilityCondition( false, errorFocusabilitySources: componentsFocusableOnError ? errorSources : null ),
							isFocused => new DisplayableElementFocusDependentData(
								attributes: isFocused ? Tuple.Create( "tabindex", "-1" ).ToCollection() : null,
								includeIdAttribute: isFocused,
								jsInitStatements: isFocused
									                  ? "document.getElementById( '{0}' ).focus(); document.getElementById( '{0}' ).scrollIntoView( {{ behavior: 'smooth', block: 'start' }} );"
										                  .FormatWith( context.Id )
									                  : "" ) ),
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