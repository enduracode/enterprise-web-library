using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using EnterpriseWebLibrary.JavaScriptWriting;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// EWL use only.
	/// </summary>
	public class ActionComponentCssElementCreator: ControlCssElementCreator {
		// This class allows us to cut the number of selectors in the ActionControlAllStyles... elements by an order of magnitude.
		internal static readonly ElementClass AllStylesClass = new ElementClass( "ewfAction" );

		internal static readonly ElementClass TextStyleClass = new ElementClass( "ewfActionText" );
		internal static readonly ElementClass ShrinkWrapButtonStyleClass = new ElementClass( "ewfActionShrinkWrapButton" );
		internal static readonly ElementClass NormalButtonStyleClass = new ElementClass( "ewfActionNormalButton" );
		internal static readonly ElementClass LargeButtonStyleClass = new ElementClass( "ewfActionLargeButton" );
		internal static readonly ElementClass ImageStyleClass = new ElementClass( "ewfActionImage" );

		internal static readonly ElementClass NewContentClass = new ElementClass( "ewfNc" );

		/// <summary>
		/// EWL use only.
		/// </summary>
		public static readonly IReadOnlyCollection<string> Selectors =
			getElementsForAllStates( "", "." + AllStylesClass.ClassName ).Single( i => i.Name == "AllStates" ).Selectors.ToImmutableArray();

		private static IEnumerable<CssElement> getElementsForAllStates( string baseName, params string[] styleSelectors ) {
			const string actionless = ":not([href]):not([" + JsWritingMethods.onclick + "])";
			var normal = ( ":not(:focus):not(:hover):not(:active)", ":not(:visited)" );
			var normalWithoutNotVisited = ( ":not(:focus):not(:hover):not(:active)", "" ); // seems to fix an issue in which IE 11 ignores background-color
			var visited = ( ":visited:not(:focus):not(:hover):not(:active)", "" );
			var focus = ( ":focus:not(:active)", "" );
			var focusNoHover = ( ":focus:not(:hover):not(:active)", "" );
			var hover = ( ":hover:not(:active)", "" );
			var active = ( ":active", "" );

			yield return
				getStateElement(
					baseName,
					"AllStates",
					"",
					"",
					styleSelectors,
					null,
					actionless,
					normal,
					normalWithoutNotVisited,
					visited,
					focus,
					focusNoHover,
					hover,
					active );
			yield return getStateElement( baseName, "ActionlessState", "", "", styleSelectors, null, actionless );

			foreach( var newContent in new bool?[] { null, false, true } ) {
				yield return
					getStateElement(
						baseName,
						"AllActionStates",
						"AllNormalContentActionStates",
						"AllNewContentActionStates",
						styleSelectors,
						newContent,
						null,
						normal,
						normalWithoutNotVisited,
						visited,
						focus,
						focusNoHover,
						hover,
						active );
				yield return getStateElement( baseName, "AllNormalStates", "NormalContentNormalState", "NewContentNormalState", styleSelectors, newContent, null, normal );
				yield return
					getStateElement( baseName, "AllVisitedStates", "NormalContentVisitedState", "NewContentVisitedState", styleSelectors, newContent, null, visited );

				// focus with or without hover
				yield return
					getStateElement(
						baseName,
						"StatesWithFocus",
						"StatesWithNormalContentAndWithFocus",
						"StatesWithNewContentAndWithFocus",
						styleSelectors,
						newContent,
						null,
						focus,
						focusNoHover,
						active );
				yield return
					getStateElement(
						baseName,
						"StatesWithFocusAndWithoutActive",
						"StatesWithNormalContentAndWithFocusAndWithoutActive",
						"StatesWithNewContentAndWithFocusAndWithoutActive",
						styleSelectors,
						newContent,
						null,
						focus,
						focusNoHover );

				// Focus without hover. Hover should trump focus according to http://meyerweb.com/eric/thoughts/2007/06/04/ordering-the-link-states/.
				yield return
					getStateElement(
						baseName,
						"StatesWithFocusAndWithoutHover",
						"StatesWithNormalContentAndWithFocusAndWithoutHover",
						"StatesWithNewContentAndWithFocusAndWithoutHover",
						styleSelectors,
						newContent,
						null,
						focusNoHover,
						active );
				yield return
					getStateElement(
						baseName,
						"StatesWithFocusAndWithoutHoverAndWithoutActive",
						"NormalContentFocusNoHoverState",
						"NewContentFocusNoHoverState",
						styleSelectors,
						newContent,
						null,
						focusNoHover );

				// hover with or without focus
				yield return
					getStateElement(
						baseName,
						"StatesWithHover",
						"StatesWithNormalContentAndWithHover",
						"StatesWithNewContentAndWithHover",
						styleSelectors,
						newContent,
						null,
						hover,
						active );
				yield return
					getStateElement(
						baseName,
						"StatesWithHoverAndWithoutActive",
						"StatesWithNormalContentAndWithHoverAndWithoutActive",
						"StatesWithNewContentAndWithHoverAndWithoutActive",
						styleSelectors,
						newContent,
						null,
						hover );

				yield return getStateElement( baseName, "AllActiveStates", "NormalContentActiveState", "NewContentActiveState", styleSelectors, newContent, null, active );
			}
		}

		private static CssElement getStateElement(
			string baseName, string allContentName, string normalContentName, string newContentName, IEnumerable<string> styleSelectors, bool? newContent,
			string actionlessSelector, params ( string, string )[] actionStateSelectors ) {
			var newContentSelector = "." + NewContentClass.ClassName;
			var normalContentSelector = ":not(" + newContentSelector + ")";
			var contentSelectors = newContent.HasValue
				                       ? newContent.Value ? newContentSelector.ToCollection() : normalContentSelector.ToCollection()
				                       : new[] { normalContentSelector, newContentSelector };
			var selectors = from styleSelector in styleSelectors
			                from contentSelector in ( actionlessSelector != null ? ( null as string ).ToCollection() : new string[ 0 ] ).Concat( contentSelectors )
			                from stateSelector in contentSelector == null ? ( general: actionlessSelector, anchorOnly: "" ).ToCollection() : actionStateSelectors
			                from selector in contentSelector == null || stateSelector.general.StartsWith( ":visited" )
				                                 ? getHyperlinkSelector( styleSelector, contentSelector ?? "", stateSelector.general ).ToCollection()
				                                 : ( baseName.StartsWith( "Button" )
					                                     ? Enumerable.Empty<string>()
					                                     : getActionHyperlinkSelectors(
						                                     styleSelector,
						                                     contentSelector,
						                                     stateSelector.anchorOnly + stateSelector.general ) ).Concat(
					                                 baseName.StartsWith( "Hyperlink" )
						                                 ? Enumerable.Empty<string>()
						                                 : getButtonSelector( styleSelector, contentSelector, stateSelector.general ).ToCollection() )
			                select selector;

			return new CssElement( baseName + ( newContent.HasValue ? newContent.Value ? newContentName : normalContentName : allContentName ), selectors.ToArray() );
		}

		private static string getHyperlinkSelector( string style, string content, string state ) => "a" + style + content + state;

		private static IEnumerable<string> getActionHyperlinkSelectors( string style, string content, string state ) {
			yield return "a" + style + "[href]" + content + state;
			yield return "a" + style + "[" + JsWritingMethods.onclick + "]" + content + state;
		}

		private static string getButtonSelector( string style, string content, string state ) => "button" + style + content + state;

		IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() {
			var buttonSizes = new[] { ShrinkWrapButtonStyleClass.ClassName, NormalButtonStyleClass.ClassName, LargeButtonStyleClass.ClassName };
			return new[]
					{
						getElementsForAllStates( "ActionComponentAllStyles", ".{0}".FormatWith( AllStylesClass.ClassName ) ),
						getElementsForAllStates( "HyperlinkStandardStyle", ".{0}".FormatWith( TextStyleClass.ClassName ) ),
						getElementsForAllStates( "ActionComponentAllButtonStyles", buttonSizes.Select( i => ".{0}".FormatWith( i ) ).ToArray() ),
						getElementsForAllStates( "ActionComponentShrinkWrapButtonStyle", ".{0}".FormatWith( ShrinkWrapButtonStyleClass.ClassName ) ),
						getElementsForAllStates( "ActionComponentNormalButtonStyle", ".{0}".FormatWith( NormalButtonStyleClass.ClassName ) ),
						getElementsForAllStates( "ActionComponentLargeButtonStyle", ".{0}".FormatWith( LargeButtonStyleClass.ClassName ) ),
						getElementsForAllStates( "ButtonAllStandardStyles", buttonSizes.Select( i => ".{0}".FormatWith( i ) ).ToArray() ),
						getElementsForAllStates( "ButtonShrinkWrapStandardStyle", ".{0}".FormatWith( ShrinkWrapButtonStyleClass.ClassName ) ),
						getElementsForAllStates( "ButtonNormalStandardStyle", ".{0}".FormatWith( NormalButtonStyleClass.ClassName ) ),
						getElementsForAllStates( "ButtonLargeStandardStyle", ".{0}".FormatWith( LargeButtonStyleClass.ClassName ) ),
						getElementsForAllStates( "HyperlinkAllButtonStyles", buttonSizes.Select( i => ".{0}".FormatWith( i ) ).ToArray() ),
						getElementsForAllStates( "HyperlinkShrinkWrapButtonStyle", ".{0}".FormatWith( ShrinkWrapButtonStyleClass.ClassName ) ),
						getElementsForAllStates( "HyperlinkNormalButtonStyle", ".{0}".FormatWith( NormalButtonStyleClass.ClassName ) ),
						getElementsForAllStates( "HyperlinkLargeButtonStyle", ".{0}".FormatWith( LargeButtonStyleClass.ClassName ) ),
						getElementsForAllStates( "ActionComponentImageStyle", ".{0}".FormatWith( ImageStyleClass.ClassName ) )
					}.SelectMany( i => i )
				.Materialize();
		}
	}
}