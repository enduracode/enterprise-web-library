using System.Collections.Generic;
using System.Linq;
using RedStapler.StandardLibrary.EnterpriseWebFramework.CssHandling;
using RedStapler.StandardLibrary.JavaScriptWriting;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// Standard Library use only.
	/// </summary>
	public class CssElementCreator: ControlCssElementCreator {
		// This class allows us to cut the number of selectors in the ActionControlAllStyles... elements by an order of magnitude.
		internal const string AllStylesClass = "ewfAction";

		internal const string BoxStyleClass = "ewfActionBox";
		internal const string BoxStyleSideAndBackgroundImageBoxClass = "ewfActionBox1";
		internal const string BoxStyleTextClass = "ewfActionBox2";
		internal const string ShrinkWrapButtonStyleClass = "ewfActionShrinkWrapButton";
		internal const string NormalButtonStyleClass = "ewfActionNormalButton";
		internal const string LargeButtonStyleClass = "ewfActionLargeButton";
		internal const string ImageStyleClass = "ewfActionImage";
		internal const string TextStyleClass = "ewfActionText";

		/// <summary>
		/// Standard Library use only.
		/// </summary>
		public static readonly string[] Selectors = getElementsForAllStates( "", "." + AllStylesClass ).Single( i => i.Name == "AllStates" ).Selectors.ToArray();

		CssElement[] ControlCssElementCreator.CreateCssElements() {
			return
				new[]
					{
						getElementsForAllStates( "ActionControlAllStyles", "." + AllStylesClass ), getElementsForAllStates( "ActionControlBoxStyle", "." + BoxStyleClass ),
						new CssElement( "ActionControlBoxStyleSideAndBackgroundImageBox", "span." + BoxStyleSideAndBackgroundImageBoxClass ).ToSingleElementArray(),
						new CssElement( "ActionControlBoxStyleText", "span." + BoxStyleTextClass ).ToSingleElementArray(),
						getElementsForAllStates( "ActionControlAllButtonStyles", "." + ShrinkWrapButtonStyleClass, "." + NormalButtonStyleClass, "." + LargeButtonStyleClass ),
						getElementsForAllStates( "ActionControlShrinkWrapButtonStyle", "." + ShrinkWrapButtonStyleClass ),
						getElementsForAllStates( "ActionControlNormalButtonStyle", "." + NormalButtonStyleClass ),
						getElementsForAllStates( "ActionControlLargeButtonStyle", "." + LargeButtonStyleClass ),
						getElementsForAllStates( "ActionControlImageStyle", "." + ImageStyleClass ), getElementsForAllStates( "ActionControlTextStyle", "." + TextStyleClass )
					}.
					SelectMany( i => i ).ToArray();
		}

		private static IEnumerable<CssElement> getElementsForAllStates( string baseName, params string[] styleSelectors ) {
			// NOTE: Uncomment the things below when we no longer support IE7/IE8.
			var actionless = styleSelectors.Select( i => "a" + i /*+ ":not([href]):not([" + JsWritingMethods.onclick + "])"*/ );
			var normal =
				styleSelectors.SelectMany(
					i => getActionStateSelectors( i, "" /*+ ":not(:focus):not(:hover):not(:active)"*/, anchorOnlyStateSelector: "" /*+ ":not(:visited)"*/ ) );
			var visited = styleSelectors.Select( i => "a" + i + ":visited" /*+ ":not(:focus):not(:hover):not(:active)"*/ );
			var focus = styleSelectors.SelectMany( i => getActionStateSelectors( i, ":focus" /*+ ":not(:active)"*/ ) );
			var focusNoHover = styleSelectors.SelectMany( i => getActionStateSelectors( i, ":focus" /*+ ":not(:hover):not(:active)"*/ ) );
			var hover = styleSelectors.SelectMany( i => getActionStateSelectors( i, ":hover" /*+ ":not(:active)"*/ ) );
			var active = styleSelectors.SelectMany( i => getActionStateSelectors( i, ":active" ) );

			yield return
				new CssElement( baseName + "AllStates",
				                actionless.Concat( normal ).Concat( visited ).Concat( focus ).Concat( focusNoHover ).Concat( hover ).Concat( active ).ToArray() );
			yield return new CssElement( baseName + "ActionlessState", actionless.ToArray() );
			yield return
				new CssElement( baseName + "AllActionStates", normal.Concat( visited ).Concat( focus ).Concat( focusNoHover ).Concat( hover ).Concat( active ).ToArray() );
			yield return new CssElement( baseName + "NormalState", normal.ToArray() );
			yield return new CssElement( baseName + "VisitedState", visited.ToArray() );

			// focus with or without hover
			yield return new CssElement( baseName + "StatesWithFocus", focus.Concat( focusNoHover ).Concat( active ).ToArray() );
			yield return new CssElement( baseName + "StatesWithFocusAndWithoutActive", focus.Concat( focusNoHover ).ToArray() );

			// Focus without hover. Hover should trump focus according to http://meyerweb.com/eric/thoughts/2007/06/04/ordering-the-link-states/.
			yield return new CssElement( baseName + "StatesWithFocusAndWithoutHover", focusNoHover.Concat( active ).ToArray() );
			yield return new CssElement( baseName + "FocusNoHoverState", focusNoHover.ToArray() );

			// hover with or without focus
			yield return new CssElement( baseName + "StatesWithHover", hover.Concat( active ).ToArray() );
			yield return new CssElement( baseName + "StatesWithHoverAndWithoutActive", hover.ToArray() );

			yield return new CssElement( baseName + "ActiveState", active.ToArray() );
		}

		private static IEnumerable<string> getActionStateSelectors( string styleSelector, string stateSelector, string anchorOnlyStateSelector = "" ) {
			yield return "a" + styleSelector + "[href]" + anchorOnlyStateSelector + stateSelector;
			yield return "a" + styleSelector + "[" + JsWritingMethods.onclick + "]" + anchorOnlyStateSelector + stateSelector;
			yield return "button" + styleSelector + stateSelector;
		}
	}
}