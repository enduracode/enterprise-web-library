using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using EnterpriseWebLibrary.JavaScriptWriting;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// EWL use only.
	/// </summary>
	public class ActionComponentCssElementCreator: ControlCssElementCreator {
		// This class allows us to cut the number of selectors in the ActionControlAllStyles... elements by an order of magnitude.
		internal static readonly ElementClass AllStylesClass = new ElementClass( "ewfAction" );

		internal static readonly ElementClass ShrinkWrapButtonStyleClass = new ElementClass( "ewfActionShrinkWrapButton" );
		internal static readonly ElementClass NormalButtonStyleClass = new ElementClass( "ewfActionNormalButton" );
		internal static readonly ElementClass LargeButtonStyleClass = new ElementClass( "ewfActionLargeButton" );
		internal static readonly ElementClass ImageStyleClass = new ElementClass( "ewfActionImage" );
		internal static readonly ElementClass TextStyleClass = new ElementClass( "ewfActionText" );

		internal static readonly ElementClass NewContentClass = new ElementClass( "ewfNc" );

		/// <summary>
		/// EWL use only.
		/// </summary>
		public static readonly IReadOnlyCollection<string> Selectors =
			getElementsForAllStates( "", "." + AllStylesClass.ClassName ).Single( i => i.Name == "AllStates" ).Selectors.ToImmutableArray();

		private static IEnumerable<CssElement> getElementsForAllStates( string baseName, params string[] styleSelectors ) {
			const string actionless = ":not([href]):not([" + JsWritingMethods.onclick + "])";
			var normal = Tuple.Create( ":not(:focus):not(:hover):not(:active)", ":not(:visited)" );
			var normalWithoutNotVisited = Tuple.Create( ":not(:focus):not(:hover):not(:active)", "" ); // seems to fix an issue in which IE 11 ignores background-color
			var visited = Tuple.Create( ":visited:not(:focus):not(:hover):not(:active)", "" );
			var focus = Tuple.Create( ":focus:not(:active)", "" );
			var focusNoHover = Tuple.Create( ":focus:not(:hover):not(:active)", "" );
			var hover = Tuple.Create( ":hover:not(:active)", "" );
			var active = Tuple.Create( ":active", "" );

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
			string actionlessSelector, params Tuple<string, string>[] actionStateSelectors ) {
			var newContentSelector = "." + NewContentClass.ClassName;
			var normalContentSelector = ":not(" + newContentSelector + ")";
			var contentSelectors = newContent.HasValue
				                       ? newContent.Value ? newContentSelector.ToCollection() : normalContentSelector.ToCollection()
				                       : new[] { normalContentSelector, newContentSelector };
			var selectors = from styleSelector in styleSelectors
			                from contentSelector in ( actionlessSelector != null ? ( null as string ).ToCollection() : new string[ 0 ] ).Concat( contentSelectors )
			                from stateSelector in contentSelector == null ? Tuple.Create( actionlessSelector, "" ).ToCollection() : actionStateSelectors
			                from selectorGetter in
				                contentSelector == null || stateSelector.Item1.StartsWith( ":visited" )
					                ? new Func<string, string, string, string, string>[]
						                { ( style, content, anchorOnlyState, state ) => "a" + style + ( content ?? "" ) + state }
					                : new Func<string, string, string, string, string>[]
						                {
							                ( style, content, anchorOnlyState, state ) => "a" + style + "[href]" + content + anchorOnlyState + state,
							                ( style, content, anchorOnlyState, state ) => "a" + style + "[" + JsWritingMethods.onclick + "]" + content + anchorOnlyState + state,
							                ( style, content, anchorOnlyState, state ) => "button" + style + content + state
						                }
			                select selectorGetter( styleSelector, contentSelector, stateSelector.Item2, stateSelector.Item1 );

			return new CssElement( baseName + ( newContent.HasValue ? newContent.Value ? newContentName : normalContentName : allContentName ), selectors.ToArray() );
		}

		IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() {
			return
				new[]
					{
						getElementsForAllStates( "ActionControlAllStyles", "." + AllStylesClass.ClassName ),
						getElementsForAllStates(
							"ActionControlAllButtonStyles",
							"." + ShrinkWrapButtonStyleClass.ClassName,
							"." + NormalButtonStyleClass.ClassName,
							"." + LargeButtonStyleClass.ClassName ),
						getElementsForAllStates( "ActionControlShrinkWrapButtonStyle", "." + ShrinkWrapButtonStyleClass.ClassName ),
						getElementsForAllStates( "ActionControlNormalButtonStyle", "." + NormalButtonStyleClass.ClassName ),
						getElementsForAllStates( "ActionControlLargeButtonStyle", "." + LargeButtonStyleClass.ClassName ),
						getElementsForAllStates( "ActionControlImageStyle", "." + ImageStyleClass.ClassName ),
						getElementsForAllStates( "ActionControlTextStyle", "." + TextStyleClass.ClassName )
					}.SelectMany( i => i ).ToArray();
		}
	}
}