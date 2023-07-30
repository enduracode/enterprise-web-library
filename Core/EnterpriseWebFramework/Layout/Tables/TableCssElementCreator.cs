#nullable disable
namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// EWL use only.
	/// </summary>
	public class TableCssElementCreator: ControlCssElementCreator {
		internal static readonly ElementClass StandardLayoutOnlyStyleClass = new( "ewfStandardLayoutOnly" );
		internal static readonly ElementClass StandardExceptLayoutStyleClass = new( "ewfTblSel" );
		internal static readonly ElementClass StandardStyleClass = new( "ewfStandard" );

		// This class allows the cell selectors to have the same specificity as the text alignment and cell alignment rules in the EWF CSS files.
		internal static readonly ElementClass AllCellAlignmentsClass = new( "ewfTc" );

		internal static readonly ElementClass ItemLimitingAndGeneralActionContainerClass = new( "ewfTblIlga" );
		internal static readonly ElementClass ItemLimitingControlContainerClass = new( "ewfTblIl" );
		internal static readonly ElementClass ItemSelectionAndActionContainerClass = new( "ewfTblIsa" );
		internal static readonly ElementClass ItemSelectionLabelAndControlContainerClass = new( "ewfTblIs" );
		internal static readonly ElementClass ItemSelectionLabelClass = new( "ewfTblIsl" );
		internal static readonly ElementClass ItemSelectionControlContainerClass = new( "ewfTblIsc" );
		internal static readonly ElementClass ItemGroupNameAndGeneralActionContainerClass = new( "ewfTblIgnga" );
		internal static readonly ElementClass ActionListContainerClass = new( "ewfTblAl" );

		internal static readonly ElementClass ContrastClass = new( "ewfContrast" );

		IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() {
			var elements = new[]
				{
					new CssElement(
						"TableAllStyles",
						"table",
						"table." + StandardLayoutOnlyStyleClass.ClassName,
						"table." + StandardExceptLayoutStyleClass.ClassName,
						"table." + StandardStyleClass.ClassName ),
					new CssElement(
						"TableStandardAndStandardLayoutOnlyStyles",
						"table." + StandardStyleClass.ClassName,
						"table." + StandardLayoutOnlyStyleClass.ClassName ),
					new CssElement(
						"TableStandardAndStandardExceptLayoutStyles",
						"table." + StandardStyleClass.ClassName,
						"table." + StandardExceptLayoutStyleClass.ClassName ),
					new CssElement( "TableStandardStyle", "table." + StandardStyleClass.ClassName ),
					new CssElement( "TheadAndTfootAndTbody", "thead", "tfoot", "tbody" ),
					new CssElement( "ThAndTd", ( from e in new[] { "th", "td" } select e + "." + AllCellAlignmentsClass.ClassName ).ToArray() ),
					new CssElement( "Th", "th." + AllCellAlignmentsClass.ClassName ), new CssElement( "Td", "td." + AllCellAlignmentsClass.ClassName ),
					new CssElement( "TableItemLimitingAndGeneralActionContainer", "div.{0}".FormatWith( ItemLimitingAndGeneralActionContainerClass.ClassName ) ),
					new CssElement( "TableItemLimitingControlContainer", "div.{0}".FormatWith( ItemLimitingControlContainerClass.ClassName ) ),
					new CssElement( "TableItemSelectionAndActionContainer", "div.{0}".FormatWith( ItemSelectionAndActionContainerClass.ClassName ) ),
					new CssElement( "TableItemSelectionLabelAndControlContainer", "div.{0}".FormatWith( ItemSelectionLabelAndControlContainerClass.ClassName ) ),
					new CssElement( "TableItemSelectionLabel", "span.{0}".FormatWith( ItemSelectionLabelClass.ClassName ) ),
					new CssElement( "TableItemSelectionControlContainer", "div.{0}".FormatWith( ItemSelectionControlContainerClass.ClassName ) ),
					new CssElement( "TableItemGroupNameAndGeneralActionContainer", "div.{0}".FormatWith( ItemGroupNameAndGeneralActionContainerClass.ClassName ) ),
					new CssElement( "TableActionListContainer", "div.{0}".FormatWith( ActionListContainerClass.ClassName ) )
				}.ToList();


			// Add row elements.

			const string tr = "tr";
			var noActionSelector = ":not(." + ElementActivationBehavior.ActivatableClass.ClassName + ")";
			var actionSelector = "." + ElementActivationBehavior.ActivatableClass.ClassName;
			const string noHoverSelector = ":not(:hover)";
			const string hoverSelector = ":hover";
			var contrastSelector = "." + ContrastClass.ClassName;

			var trNoAction = tr + noActionSelector;
			var trNoActionContrast = tr + noActionSelector + contrastSelector;
			var trActionNoHover = tr + actionSelector + noHoverSelector;
			var trActionNoHoverContrast = tr + actionSelector + noHoverSelector + contrastSelector;
			var trActionHover = tr + actionSelector + hoverSelector;
			var trActionHoverContrast = tr + actionSelector + hoverSelector + contrastSelector;

			// all rows
			elements.Add(
				new CssElement( "TrAllStates", trNoAction, trNoActionContrast, trActionNoHover, trActionNoHoverContrast, trActionHover, trActionHoverContrast ) );
			elements.Add( new CssElement( "TrStatesWithContrast", trNoActionContrast, trActionNoHoverContrast, trActionHoverContrast ) );

			// all rows except the one being hovered, if it's an action row
			elements.Add( new CssElement( "TrStatesWithNoActionHover", trNoAction, trNoActionContrast, trActionNoHover, trActionNoHoverContrast ) );
			elements.Add( new CssElement( "TrStatesWithNoActionHoverAndWithContrast", trNoActionContrast, trActionNoHoverContrast ) );

			// non action rows
			elements.Add( new CssElement( "TrStatesWithNoAction", trNoAction, trNoActionContrast ) );
			elements.Add( new CssElement( "TrStatesWithNoActionAndWithContrast", trNoActionContrast ) );

			// action rows
			elements.Add( new CssElement( "TrStatesWithAction", trActionNoHover, trActionNoHoverContrast, trActionHover, trActionHoverContrast ) );
			elements.Add( new CssElement( "TrStatesWithActionAndWithContrast", trActionNoHoverContrast, trActionHoverContrast ) );

			// action rows except the one being hovered
			elements.Add( new CssElement( "TrStatesWithActionAndWithNoHover", trActionNoHover, trActionNoHoverContrast ) );
			elements.Add( new CssElement( "TrStatesWithActionAndWithNoHoverAndWithContrast", trActionNoHoverContrast ) );

			// the action row being hovered
			elements.Add( new CssElement( "TrStatesWithActionAndWithHover", trActionHover, trActionHoverContrast ) );

			return elements;
		}
	}
}