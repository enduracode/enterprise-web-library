using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Humanizer;
using StackExchange.Profiling;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A table. Do not use this control in markup.
	/// </summary>
	public class EwfTable: WebControl, ControlTreeDataLoader {
		private const string itemLimitPageStateKey = "itemLimit";

		/// <summary>
		/// EWL use only.
		/// </summary>
		public class CssElementCreator: ControlCssElementCreator {
			internal const string StandardLayoutOnlyStyleClass = "ewfStandardLayoutOnly";
			internal const string StandardExceptLayoutStyleClass = "ewfTblSel";
			internal const string StandardStyleClass = "ewfStandard";

			// This class allows the cell selectors to have the same specificity as the text alignment and cell alignment rules in the EWF CSS files.
			internal const string AllCellAlignmentsClass = "ewfTc";

			// NOTE: Rename ewfClickable to ewfAction and try to restrict its use to table rows since action controls have CSS elements that we can use for styling.
			private const string actionClass = "ewfClickable"; // NOTE: Where will this be used besides here?

			internal const string ContrastClass = "ewfContrast";

			/// <summary>
			/// EWL use only.
			/// </summary>
			public static readonly string[] Selectors =
				{
					"table", "table." + StandardLayoutOnlyStyleClass, "table." + StandardExceptLayoutStyleClass,
					"table." + StandardStyleClass
				};

			internal static readonly string[] CellSelectors = ( from e in new[] { "th", "td" } select e + "." + AllCellAlignmentsClass ).ToArray();

			CssElement[] ControlCssElementCreator.CreateCssElements() {
				var elements =
					new[]
						{
							new CssElement( "TableAllStyles", Selectors ),
							new CssElement( "TableStandardAndStandardLayoutOnlyStyles", "table." + StandardStyleClass, "table." + StandardLayoutOnlyStyleClass ),
							new CssElement( "TableStandardAndStandardExceptLayoutStyles", "table." + StandardStyleClass, "table." + StandardExceptLayoutStyleClass ),
							new CssElement( "TableStandardStyle", "table." + StandardStyleClass ), new CssElement( "TheadAndTfootAndTbody", "thead", "tfoot", "tbody" ),
							new CssElement( "ThAndTd", CellSelectors ), new CssElement( "Th", "th." + AllCellAlignmentsClass ),
							new CssElement( "Td", "td." + AllCellAlignmentsClass )
						}.ToList();


				// Add row elements.

				const string tr = "tr";
				const string noActionSelector = ":not(." + actionClass + ")";
				const string actionSelector = "." + actionClass;
				const string noHoverSelector = ":not(:hover)";
				const string hoverSelector = ":hover";
				const string contrastSelector = "." + ContrastClass;

				const string trNoAction = tr + noActionSelector;
				const string trNoActionContrast = tr + noActionSelector + contrastSelector;
				const string trActionNoHover = tr + actionSelector + noHoverSelector;
				const string trActionNoHoverContrast = tr + actionSelector + noHoverSelector + contrastSelector;
				const string trActionHover = tr + actionSelector + hoverSelector;
				const string trActionHoverContrast = tr + actionSelector + hoverSelector + contrastSelector;

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

				return elements.ToArray();
			}
		}

		internal static void SetUpTableAndCaption( WebControl table, EwfTableStyle style, ReadOnlyCollection<string> classes, string caption, string subCaption ) {
			table.CssClass = StringTools.ConcatenateWithDelimiter( " ", new[] { getTableStyleClass( style ) }.Concat( classes ).ToArray() );
			addCaptionIfNecessary( table, caption, subCaption );
		}

		private static string getTableStyleClass( EwfTableStyle style ) {
			switch( style ) {
				case EwfTableStyle.StandardLayoutOnly:
					return CssElementCreator.StandardLayoutOnlyStyleClass;
				case EwfTableStyle.StandardExceptLayout:
					return CssElementCreator.StandardExceptLayoutStyleClass;
				case EwfTableStyle.Standard:
					return CssElementCreator.StandardStyleClass;
				default:
					return "";
			}
		}

		private static void addCaptionIfNecessary( WebControl table, string caption, string subCaption ) {
			if( caption.Length == 0 )
				return;
			var subCaptionControls = new List<Control>();
			if( subCaption.Length > 0 )
				subCaptionControls.AddRange( new Control[] { new LineBreak(), subCaption.GetLiteralControl() } );
			table.Controls.Add(
				new WebControl( HtmlTextWriterTag.Caption ).AddControlsReturnThis( new Control[] { caption.GetLiteralControl() }.Concat( subCaptionControls ) ) );
		}

		internal static EwfTableField[] GetFields( EwfTableField[] fields, ReadOnlyCollection<EwfTableItem> headItems, IEnumerable<EwfTableItem> items ) {
			var firstSpecifiedItem = headItems.Concat( items ).FirstOrDefault();
			if( firstSpecifiedItem == null )
				return new EwfTableField[ 0 ];

			if( fields != null )
				return fields;

			// Set the fields up implicitly, based on the first item, if they weren't specified explicitly.
			var fieldCount = firstSpecifiedItem.Cells.Sum( i => i.Setup.FieldSpan );
			return Enumerable.Repeat( new EwfTableField(), fieldCount ).ToArray();
		}

		internal static double GetColumnWidthFactor( IEnumerable<EwfTableFieldOrItemSetup> fieldOrItemSetups ) {
			if( fieldOrItemSetups.Any( f => f.Size.IsEmpty ) )
				return 1;
			return 100 / fieldOrItemSetups.Where( f => f.Size.Type == UnitType.Percentage ).Sum( f => f.Size.Value );
		}

		internal static WebControl GetColControl( EwfTableFieldOrItemSetup fieldOrItemSetup, double columnWidthFactor ) {
			var width = fieldOrItemSetup.Size;
			return new WebControl( HtmlTextWriterTag.Col )
				{
					Width = !width.IsEmpty && width.Type == UnitType.Percentage ? Unit.Percentage( width.Value * columnWidthFactor ) : width
				};
		}

		internal static void AssertAtLeastOneCellPerField( EwfTableField[] fields, List<List<CellPlaceholder>> cellPlaceholderListsForItems ) {
			// If there is absolutely nothing in the table, we must bypass the assertion since it will always throw an exception.
			if( !cellPlaceholderListsForItems.Any() )
				return;

			// Enforce that there is at least one cell in each field by looking at array of all items.
			for( var fieldIndex = 0; fieldIndex < fields.Length; fieldIndex += 1 ) {
				if( !cellPlaceholderListsForItems.Select( i => i[ fieldIndex ] ).OfType<EwfTableCell>().Any() )
					throw new ApplicationException( "The field with index " + fieldIndex + " does not have any cells." );
			}
		}

		/// <summary>
		/// Creates a table with one empty item group.
		/// </summary>
		/// <param name="hideIfEmpty">Set to true if you want this table to hide itself if it has no content rows.</param>
		/// <param name="style">The table's style.</param>
		/// <param name="classes">The classes on the table.</param>
		/// <param name="postBackIdBase">Do not pass null.</param>
		/// <param name="caption">The caption that appears above the table. Do not pass null. Setting this to the empty string means the table will have no caption.
		/// </param>
		/// <param name="subCaption">The sub caption that appears directly under the caption. Do not pass null. Setting this to the empty string means there will be
		/// no sub caption.</param>
		/// <param name="allowExportToExcel">Set to true if you want an Export to Excel action link to appear. This will only work if the table consists of simple
		/// text (no controls).</param>
		/// <param name="tableActions">Table action buttons. This could be used to add a new customer or other entity to the table, for example.</param>
		/// <param name="fields">The table's fields. Do not pass an empty array.</param>
		/// <param name="headItems">The table's head items.</param>
		/// <param name="defaultItemLimit">The maximum number of result items that will be shown. Default is DataRowLimit.Unlimited. A default item limit of
		/// anything other than Unlimited will cause the table to show a control allowing the user to select how many results they want to see, as well as an
		/// indicator of the total number of results that would be shown if there was no limit.</param>
		/// <param name="disableEmptyFieldDetection">Set to true if you want to disable the "at least one cell per field" assertion. Use with caution.</param>
		/// <param name="tailUpdateRegions">The tail update regions.</param>
		public static EwfTable Create(
			bool hideIfEmpty = false, EwfTableStyle style = EwfTableStyle.Standard, IEnumerable<string> classes = null, string postBackIdBase = "", string caption = "",
			string subCaption = "", bool allowExportToExcel = false, IEnumerable<Tuple<string, Action>> tableActions = null, IEnumerable<EwfTableField> fields = null,
			IEnumerable<EwfTableItem> headItems = null, DataRowLimit defaultItemLimit = DataRowLimit.Unlimited, bool disableEmptyFieldDetection = false,
			IEnumerable<TailUpdateRegion> tailUpdateRegions = null ) {
			return new EwfTable(
				hideIfEmpty,
				style,
				classes,
				postBackIdBase,
				caption,
				subCaption,
				allowExportToExcel,
				tableActions,
				fields,
				headItems,
				defaultItemLimit,
				disableEmptyFieldDetection,
				new EwfTableItemGroup( () => new EwfTableItemGroupRemainingData( null, tailUpdateRegions: tailUpdateRegions ), ImmutableArray<Func<EwfTableItem>>.Empty )
					.ToSingleElementArray(),
				null );
		}

		/// <summary>
		/// Creates a table with one item group that contains the specified items.
		/// </summary>
		/// <param name="items">The items. Do not pass null.</param>
		/// <param name="hideIfEmpty">Set to true if you want this table to hide itself if it has no content rows.</param>
		/// <param name="style">The table's style.</param>
		/// <param name="classes">The classes on the table.</param>
		/// <param name="postBackIdBase">Do not pass null.</param>
		/// <param name="caption">The caption that appears above the table. Do not pass null. Setting this to the empty string means the table will have no caption.
		/// </param>
		/// <param name="subCaption">The sub caption that appears directly under the caption. Do not pass null. Setting this to the empty string means there will be
		/// no sub caption.</param>
		/// <param name="allowExportToExcel">Set to true if you want an Export to Excel action link to appear. This will only work if the table consists of simple
		/// text (no controls).</param>
		/// <param name="tableActions">Table action buttons. This could be used to add a new customer or other entity to the table, for example.</param>
		/// <param name="fields">The table's fields. Do not pass an empty array.</param>
		/// <param name="headItems">The table's head items.</param>
		/// <param name="defaultItemLimit">The maximum number of result items that will be shown. Default is DataRowLimit.Unlimited. A default item limit of
		/// anything other than Unlimited will cause the table to show a control allowing the user to select how many results they want to see, as well as an
		/// indicator of the total number of results that would be shown if there was no limit.</param>
		/// <param name="disableEmptyFieldDetection">Set to true if you want to disable the "at least one cell per field" assertion. Use with caution.</param>
		/// <param name="tailUpdateRegions">The tail update regions.</param>
		public static EwfTable CreateWithItems(
			IEnumerable<Func<EwfTableItem>> items, bool hideIfEmpty = false, EwfTableStyle style = EwfTableStyle.Standard, IEnumerable<string> classes = null,
			string postBackIdBase = "", string caption = "", string subCaption = "", bool allowExportToExcel = false,
			IEnumerable<Tuple<string, Action>> tableActions = null, IEnumerable<EwfTableField> fields = null, IEnumerable<EwfTableItem> headItems = null,
			DataRowLimit defaultItemLimit = DataRowLimit.Unlimited, bool disableEmptyFieldDetection = false, IEnumerable<TailUpdateRegion> tailUpdateRegions = null ) {
			return new EwfTable(
				hideIfEmpty,
				style,
				classes,
				postBackIdBase,
				caption,
				subCaption,
				allowExportToExcel,
				tableActions,
				fields,
				headItems,
				defaultItemLimit,
				disableEmptyFieldDetection,
				new EwfTableItemGroup( () => new EwfTableItemGroupRemainingData( null, tailUpdateRegions: tailUpdateRegions ), items ).ToSingleElementArray(),
				null );
		}

		/// <summary>
		/// Creates a table with multiple item groups.
		/// </summary>
		/// <param name="itemGroups">The item groups. Do not pass null.</param>
		/// <param name="hideIfEmpty">Set to true if you want this table to hide itself if it has no content rows.</param>
		/// <param name="style">The table's style.</param>
		/// <param name="classes">The classes on the table.</param>
		/// <param name="postBackIdBase">Do not pass null.</param>
		/// <param name="caption">The caption that appears above the table. Do not pass null. Setting this to the empty string means the table will have no caption.
		/// </param>
		/// <param name="subCaption">The sub caption that appears directly under the caption. Do not pass null. Setting this to the empty string means there will be
		/// no sub caption.</param>
		/// <param name="allowExportToExcel">Set to true if you want an Export to Excel action link to appear. This will only work if the table consists of simple
		/// text (no controls).</param>
		/// <param name="tableActions">Table action buttons. This could be used to add a new customer or other entity to the table, for example.</param>
		/// <param name="fields">The table's fields. Do not pass an empty array.</param>
		/// <param name="headItems">The table's head items.</param>
		/// <param name="defaultItemLimit">The maximum number of result items that will be shown. Default is DataRowLimit.Unlimited. A default item limit of
		/// anything other than Unlimited will cause the table to show a control allowing the user to select how many results they want to see, as well as an
		/// indicator of the total number of results that would be shown if there was no limit.</param>
		/// <param name="disableEmptyFieldDetection">Set to true if you want to disable the "at least one cell per field" assertion. Use with caution.</param>
		/// <param name="tailUpdateRegions">The tail update regions for the table. These operate on the item-group level, not the item level.</param>
		public static EwfTable CreateWithItemGroups(
			IEnumerable<EwfTableItemGroup> itemGroups, bool hideIfEmpty = false, EwfTableStyle style = EwfTableStyle.Standard, IEnumerable<string> classes = null,
			string postBackIdBase = "", string caption = "", string subCaption = "", bool allowExportToExcel = false,
			IEnumerable<Tuple<string, Action>> tableActions = null, IEnumerable<EwfTableField> fields = null, IEnumerable<EwfTableItem> headItems = null,
			DataRowLimit defaultItemLimit = DataRowLimit.Unlimited, bool disableEmptyFieldDetection = false, IEnumerable<TailUpdateRegion> tailUpdateRegions = null ) {
			return new EwfTable(
				hideIfEmpty,
				style,
				classes,
				postBackIdBase,
				caption,
				subCaption,
				allowExportToExcel,
				tableActions,
				fields,
				headItems,
				defaultItemLimit,
				disableEmptyFieldDetection,
				itemGroups,
				tailUpdateRegions );
		}

		private readonly bool hideIfEmpty;
		private readonly EwfTableStyle style;
		private readonly ReadOnlyCollection<string> classes;
		private readonly string postBackIdBase;
		private readonly string caption;
		private readonly string subCaption;
		private readonly bool allowExportToExcel;
		private readonly ReadOnlyCollection<Tuple<string, Action>> tableActions;
		private readonly EwfTableField[] specifiedFields;
		private readonly List<EwfTableItem> headItems;
		private readonly DataRowLimit defaultItemLimit;
		private readonly bool disableEmptyFieldDetection;
		private readonly IReadOnlyList<EwfTableItemGroup> itemGroups;
		private readonly IReadOnlyCollection<TailUpdateRegion> tailUpdateRegions;

		// NOTE: Change table actions to be IEnumerable<namedType> rather than IEnumerable<Tuple<>>.
		private EwfTable(
			bool hideIfEmpty, EwfTableStyle style, IEnumerable<string> classes, string postBackIdBase, string caption, string subCaption, bool allowExportToExcel,
			IEnumerable<Tuple<string, Action>> tableActions, IEnumerable<EwfTableField> fields, IEnumerable<EwfTableItem> headItems, DataRowLimit defaultItemLimit,
			bool disableEmptyFieldDetection, IEnumerable<EwfTableItemGroup> itemGroups, IEnumerable<TailUpdateRegion> tailUpdateRegions ) {
			this.hideIfEmpty = hideIfEmpty;
			this.style = style;
			this.classes = ( classes ?? new string[ 0 ] ).ToList().AsReadOnly();
			this.postBackIdBase = PostBack.GetCompositeId( "ewfTable", postBackIdBase );
			this.caption = caption;
			this.subCaption = subCaption;
			this.allowExportToExcel = allowExportToExcel;
			this.tableActions = ( tableActions ?? new Tuple<string, Action>[ 0 ] ).ToList().AsReadOnly();

			if( fields != null ) {
				if( !fields.Any() )
					throw new ApplicationException( "If fields are specified, there must be at least one of them." );
				specifiedFields = fields.ToArray();
			}

			this.headItems = ( headItems ?? new EwfTableItem[ 0 ] ).ToList();
			this.defaultItemLimit = defaultItemLimit;
			this.disableEmptyFieldDetection = disableEmptyFieldDetection;
			this.itemGroups = itemGroups.ToImmutableArray();
			this.tailUpdateRegions = tailUpdateRegions != null ? tailUpdateRegions.ToImmutableArray() : ImmutableArray<TailUpdateRegion>.Empty;
		}

		/// <summary>
		/// Gets the maximum number of items that will be shown in this table.
		/// </summary>
		public int CurrentItemLimit { get { return EwfPage.Instance.PageState.GetValue( this, itemLimitPageStateKey, (int)defaultItemLimit ); } }

		/// <summary>
		/// Adds all of the given data to the table by enumerating the data and translating each item into an EwfTableItem using the given itemSelector. If
		/// enumerating the data is expensive, this call will be slow. The data must be enumerated so the table can show the total number of items.
		/// </summary>
		public void AddData<T>( IEnumerable<T> data, Func<T, EwfTableItem> itemSelector ) {
			data.ToList().ForEach( d => AddItem( () => itemSelector( d ) ) );
		}

		/// <summary>
		/// Adds an item to the table. Does not defer creation of the item. Do not use this in tables that use item limiting.
		/// </summary>
		public void AddItem( EwfTableItem item ) {
			AddItem( () => item );
		}

		/// <summary>
		/// Adds an item to the table. Defers creation of the item. Do not directly or indirectly create validations inside the function if they will be added to a
		/// validation list that exists outside the function; this will likely cause your validations to execute in the wrong order or be skipped.
		/// </summary>
		public void AddItem( Func<EwfTableItem> item ) {
			if( itemGroups.Count != 1 )
				throw new ApplicationException( "The table must have exactly one item group." );
			itemGroups.Single().Items.Add( item );
		}

		void ControlTreeDataLoader.LoadData() {
			using( MiniProfiler.Current.Step( "EWF - Load table data" ) ) {
				if( hideIfEmpty && itemGroups.All( itemGroup => !itemGroup.Items.Any() ) ) {
					Visible = false;
					return;
				}

				SetUpTableAndCaption( this, style, classes, caption, subCaption );

				var visibleItemGroupsAndItems = new List<Tuple<EwfTableItemGroup, IReadOnlyCollection<EwfTableItem>>>();
				foreach( var itemGroup in itemGroups ) {
					var visibleItems = itemGroup.Items.Take( CurrentItemLimit - visibleItemGroupsAndItems.Sum( i => i.Item2.Count ) ).Select( i => i() );
					visibleItemGroupsAndItems.Add( Tuple.Create<EwfTableItemGroup, IReadOnlyCollection<EwfTableItem>>( itemGroup, visibleItems.ToImmutableArray() ) );
					if( visibleItemGroupsAndItems.Sum( i => i.Item2.Count ) == CurrentItemLimit )
						break;
				}

				var fields = GetFields( specifiedFields, headItems.AsReadOnly(), visibleItemGroupsAndItems.SelectMany( i => i.Item2 ) );
				if( !fields.Any() )
					fields = new EwfTableField().ToSingleElementArray();

				addColumnSpecifications( fields );

				var allVisibleItems = new List<EwfTableItem>();

				var itemLimitingUpdateRegionSet = new UpdateRegionSet();
				var headRows =
					buildRows(
						getItemLimitingAndGeneralActionsItem( fields.Length, itemLimitingUpdateRegionSet ).Concat( getItemActionsItem( fields.Length ) ).ToList(),
						Enumerable.Repeat( new EwfTableField(), fields.Length ).ToArray(),
						null,
						false,
						null,
						null,
						allVisibleItems ).Concat( buildRows( headItems, fields, null, true, null, null, allVisibleItems ) ).ToArray();
				if( headRows.Any() )
					Controls.Add( new WebControl( HtmlTextWriterTag.Thead ).AddControlsReturnThis( headRows ) );

				var bodyRowGroupsAndRows = new List<Tuple<WebControl, ImmutableArray<Control>>>();
				var updateRegionSetListsAndStaticRowGroupCounts = new List<Tuple<IReadOnlyCollection<UpdateRegionSet>, int>>();
				for( var visibleGroupIndex = 0; visibleGroupIndex < visibleItemGroupsAndItems.Count; visibleGroupIndex += 1 ) {
					var groupAndItems = visibleItemGroupsAndItems[ visibleGroupIndex ];
					var useContrastForFirstRow = visibleItemGroupsAndItems.Where( ( group, i ) => i < visibleGroupIndex ).Sum( i => i.Item2.Count ) % 2 == 1;
					var groupBodyRows = buildRows( groupAndItems.Item2, fields, useContrastForFirstRow, false, null, null, allVisibleItems ).ToImmutableArray();
					var rowGroup =
						new WebControl( HtmlTextWriterTag.Tbody ).AddControlsReturnThis(
							buildRows(
								groupAndItems.Item1.GetHeadItems( fields.Length ),
								Enumerable.Repeat( new EwfTableField(), fields.Length ).ToArray(),
								null,
								true,
								null,
								null,
								allVisibleItems ).Concat( new NamingPlaceholder( groupBodyRows ).ToSingleElementArray() ) );
					bodyRowGroupsAndRows.Add( Tuple.Create( rowGroup, groupBodyRows ) );

					var cachedVisibleGroupIndex = visibleGroupIndex;
					EwfPage.Instance.AddUpdateRegionLinker(
						new UpdateRegionLinker(
							rowGroup,
							"tail",
							from region in groupAndItems.Item1.RemainingData.Value.TailUpdateRegions
							let staticRowCount = itemGroups[ cachedVisibleGroupIndex ].Items.Count - region.UpdatingItemCount
							select new PreModificationUpdateRegion( region.Sets, () => groupBodyRows.Skip( staticRowCount ), staticRowCount.ToString ),
							arg => groupBodyRows.Skip( int.Parse( arg ) ) ) );

					// If item limiting is enabled, include all subsequent item groups in tail update regions since any number of items could be appended.
					if( defaultItemLimit != DataRowLimit.Unlimited ) {
						updateRegionSetListsAndStaticRowGroupCounts.Add(
							Tuple.Create<IReadOnlyCollection<UpdateRegionSet>, int>(
								groupAndItems.Item1.RemainingData.Value.TailUpdateRegions.SelectMany( i => i.Sets ).ToImmutableArray(),
								visibleGroupIndex + 1 ) );
					}
				}
				Controls.Add( new NamingPlaceholder( bodyRowGroupsAndRows.Select( i => i.Item1 ) ) );

				if( defaultItemLimit != DataRowLimit.Unlimited ) {
					var oldItemLimit = CurrentItemLimit;
					var lowerItemLimit = new Lazy<int>( () => Math.Min( oldItemLimit, CurrentItemLimit ) );

					var itemLimitingTailUpdateRegionControlGetter = new Func<int, IEnumerable<Control>>(
						staticItemCount => {
							var rowCount = 0;
							for( var groupIndex = 0; groupIndex < bodyRowGroupsAndRows.Count; groupIndex += 1 ) {
								var rows = bodyRowGroupsAndRows[ groupIndex ].Item2;
								rowCount += rows.Length;
								if( rowCount < staticItemCount )
									continue;
								return rows.Skip( rows.Length - ( rowCount - staticItemCount ) ).Concat( bodyRowGroupsAndRows.Skip( groupIndex + 1 ).Select( i => i.Item1 ) );
							}
							return ImmutableArray<Control>.Empty;
						} );

					EwfPage.Instance.AddUpdateRegionLinker(
						new UpdateRegionLinker(
							this,
							"itemLimitingTail",
							new PreModificationUpdateRegion(
								itemLimitingUpdateRegionSet.ToSingleElementArray(),
								() => itemLimitingTailUpdateRegionControlGetter( lowerItemLimit.Value ),
								() => lowerItemLimit.Value.ToString() ).ToSingleElementArray(),
							arg => itemLimitingTailUpdateRegionControlGetter( int.Parse( arg ) ) ) );
				}

				EwfPage.Instance.AddUpdateRegionLinker(
					new UpdateRegionLinker(
						this,
						"tail",
						from region in
							tailUpdateRegions.Select( i => new { sets = i.Sets, staticRowGroupCount = itemGroups.Count - i.UpdatingItemCount } )
							.Concat( updateRegionSetListsAndStaticRowGroupCounts.Select( i => new { sets = i.Item1, staticRowGroupCount = i.Item2 } ) )
						select
							new PreModificationUpdateRegion(
							region.sets,
							() => bodyRowGroupsAndRows.Skip( region.staticRowGroupCount ).Select( i => i.Item1 ),
							region.staticRowGroupCount.ToString ),
						arg => bodyRowGroupsAndRows.Skip( int.Parse( arg ) ).Select( i => i.Item1 ) ) );

				var itemCount = itemGroups.Sum( i => i.Items.Count );
				var itemLimitingRowGroup = new List<Control>();
				if( CurrentItemLimit < itemCount ) {
					var nextLimit = EnumTools.GetValues<DataRowLimit>().First( i => i > (DataRowLimit)CurrentItemLimit );
					var itemIncrementCount = Math.Min( (int)nextLimit, itemCount ) - CurrentItemLimit;
					var validationDm = EwfPage.Instance.DataUpdate; // This is a hack, but should be easily corrected when EnduraCode goal 768 is done.
					var button =
						new PostBackButton(
							PostBack.CreateIntermediate(
								itemLimitingUpdateRegionSet.ToSingleElementArray(),
								validationDm,
								id: PostBack.GetCompositeId( postBackIdBase, "showMore" ),
								firstModificationMethod: () => EwfPage.Instance.PageState.SetValue( this, itemLimitPageStateKey, (int)nextLimit ) ),
							new TextActionControlStyle( "Show " + itemIncrementCount + " more item" + ( itemIncrementCount != 1 ? "s" : "" ) ),
							usesSubmitBehavior: false );
					var item = new EwfTableItem( button.ToCell( new TableCellSetup( fieldSpan: fields.Length ) ) );
					var useContrast = visibleItemGroupsAndItems.Sum( i => i.Item2.Count ) % 2 == 1;
					itemLimitingRowGroup.Add(
						new WebControl( HtmlTextWriterTag.Tbody ).AddControlsReturnThis(
							buildRows(
								item.ToSingleElementArray().ToList(),
								Enumerable.Repeat( new EwfTableField(), fields.Length ).ToArray(),
								useContrast,
								false,
								null,
								null,
								allVisibleItems ) ) );
				}
				Controls.Add(
					new NamingPlaceholder(
						itemLimitingRowGroup,
						updateRegionSets:
							itemLimitingUpdateRegionSet.ToSingleElementArray()
								.Concat( itemGroups.SelectMany( i => i.RemainingData.Value.TailUpdateRegions ).Concat( tailUpdateRegions ).SelectMany( i => i.Sets ) ) ) );

				// Assert that every visible item in the table has the same number of cells and store a data structure for below.
				var cellPlaceholderListsForItems = TableOps.BuildCellPlaceholderListsForItems( allVisibleItems, fields.Length );

				if( !disableEmptyFieldDetection )
					AssertAtLeastOneCellPerField( fields, cellPlaceholderListsForItems );
			}
		}

		private void addColumnSpecifications( EwfTableField[] fields ) {
			var fieldOrItemSetups = fields.Select( i => i.FieldOrItemSetup );
			var factor = GetColumnWidthFactor( fieldOrItemSetups );
			this.AddControlsReturnThis( fieldOrItemSetups.Select( f => GetColControl( f, factor ) ) );
		}

		// NOTE: This row also needs to include general actions, on the right. Don't forget about Export to Excel.
		private EwfTableItem[] getItemLimitingAndGeneralActionsItem( int fieldCount, UpdateRegionSet itemLimitingUpdateRegionSet ) {
			if( defaultItemLimit == DataRowLimit.Unlimited )
				return new EwfTableItem[ 0 ];

			var itemCount = itemGroups.Sum( i => i.Items.Count );
			var cl =
				new ControlLine(
					new NamingPlaceholder(
						"Item".ToQuantity( itemCount ).GetLiteralControl().ToSingleElementArray(),
						updateRegionSets: itemGroups.SelectMany( i => i.RemainingData.Value.TailUpdateRegions ).Concat( tailUpdateRegions ).SelectMany( i => i.Sets ) ),
					"".GetLiteralControl(),
					"Show:".GetLiteralControl() );
			cl.AddControls( getItemLimitButton( DataRowLimit.Fifty, itemLimitingUpdateRegionSet ) );
			cl.AddControls( getItemLimitButton( DataRowLimit.FiveHundred, itemLimitingUpdateRegionSet ) );
			cl.AddControls( getItemLimitButton( DataRowLimit.Unlimited, itemLimitingUpdateRegionSet ) );
			return new EwfTableItem( cl.ToCell( new TableCellSetup( fieldSpan: fieldCount ) ) ).ToSingleElementArray();
		}

		private Control getItemLimitButton( DataRowLimit itemLimit, UpdateRegionSet updateRegionSet ) {
			var text = itemLimit == DataRowLimit.Unlimited ? "All" : ( (int)itemLimit ).ToString();
			if( itemLimit == (DataRowLimit)CurrentItemLimit )
				return text.GetLiteralControl();
			var validationDm = EwfPage.Instance.DataUpdate; // This is a hack, but should be easily corrected when EnduraCode goal 768 is done.
			return
				new PostBackButton(
					PostBack.CreateIntermediate(
						updateRegionSet.ToSingleElementArray(),
						validationDm,
						id: PostBack.GetCompositeId( postBackIdBase, itemLimit.ToString() ),
						firstModificationMethod: () => EwfPage.Instance.PageState.SetValue( this, itemLimitPageStateKey, (int)itemLimit ) ),
					new TextActionControlStyle( text ),
					usesSubmitBehavior: false );
		}

		private EwfTableItem[] getItemActionsItem( int fieldCount ) {
			// NOTE: Build a head group row for check box selection (all/none) and check box actions, if evaluated items have check box actions.
			// NOTE: Go through all visible items and build a list of their tablewide check box actions. Make sure all items have identical lists.
			// NOTE: Make sure every item in the list has the same action names. If this holds, draw the row.
			// NOTE: Check box actions should show an error if clicked and no items are selected; this caused confusion in M+Vision.
			return new EwfTableItem[ 0 ];
		}

		private IEnumerable<Control> buildRows(
			IReadOnlyCollection<EwfTableItem> items, IReadOnlyCollection<EwfTableField> fields, bool? useContrastForFirstRow, bool useHeadCells,
			Func<EwfTableCell> itemActionCheckBoxCellGetter, Func<EwfTableCell> itemReorderingCellGetter, List<EwfTableItem> allVisibleItems ) {
			// Assert that the cells in the list of items are valid and store a data structure for below.
			var cellPlaceholderListsForRows = TableOps.BuildCellPlaceholderListsForItems( items, fields.Count );

			// NOTE: Be sure to take check box and reordering columns into account.
			var rows = TableOps.BuildRows(
				cellPlaceholderListsForRows,
				items.Select( i => i.Setup.FieldOrItemSetup ).ToImmutableArray(),
				useContrastForFirstRow,
				fields.Select( i => i.FieldOrItemSetup ).ToImmutableArray(),
				useHeadCells ? fields.Count : 0,
				false );

			allVisibleItems.AddRange( items );
			return rows;
		}

		/// <summary>
		/// Returns the table element, which represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Table; } }
	}
}