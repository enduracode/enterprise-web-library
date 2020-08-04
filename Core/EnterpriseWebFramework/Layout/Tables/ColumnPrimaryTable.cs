using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using EnterpriseWebLibrary.IO;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A column-primary table.
	/// </summary>
	public class ColumnPrimaryTable: ColumnPrimaryTable<int> {
		/// <summary>
		/// Creates a table.
		/// </summary>
		/// <param name="displaySetup"></param>
		/// <param name="style">The table's style.</param>
		/// <param name="classes">The classes on the table.</param>
		/// <param name="postBackIdBase">Do not pass null.</param>
		/// <param name="caption">The caption that appears above the table. Do not pass null. Setting this to the empty string means the table will have no caption.
		/// </param>
		/// <param name="subCaption">The sub caption that appears directly under the caption. Do not pass null. Setting this to the empty string means there will be
		/// no sub caption.</param>
		/// <param name="allowExportToExcel">Set to true if you want an Export to Excel action component to appear. This will only work if the table consists of
		/// simple text (no controls).</param>
		/// <param name="tableActions">Table action components. This could be used to add a new customer or other entity to the table, for example.</param>
		/// <param name="fields">The table's fields. Do not pass an empty collection.</param>
		/// <param name="headItems">The table's head items.</param>
		/// <param name="firstDataFieldIndex">The index of the first data field.</param>
		/// <param name="etherealContent"></param>
		public static ColumnPrimaryTable Create(
			DisplaySetup displaySetup = null, EwfTableStyle style = EwfTableStyle.Standard, ElementClassSet classes = null, string postBackIdBase = "",
			string caption = "", string subCaption = "", bool allowExportToExcel = false, IReadOnlyCollection<ActionComponentSetup> tableActions = null,
			IReadOnlyCollection<EwfTableField> fields = null, IReadOnlyCollection<EwfTableItem> headItems = null, int firstDataFieldIndex = 0,
			IReadOnlyCollection<EtherealComponent> etherealContent = null ) =>
			new ColumnPrimaryTable(
				displaySetup,
				style,
				classes,
				postBackIdBase,
				caption,
				subCaption,
				allowExportToExcel,
				tableActions,
				fields,
				headItems,
				firstDataFieldIndex,
				etherealContent );

		/// <summary>
		/// Creates a table with a specified item ID type.
		/// </summary>
		/// <param name="displaySetup"></param>
		/// <param name="style">The table's style.</param>
		/// <param name="classes">The classes on the table.</param>
		/// <param name="postBackIdBase">Do not pass null.</param>
		/// <param name="caption">The caption that appears above the table. Do not pass null. Setting this to the empty string means the table will have no caption.
		/// </param>
		/// <param name="subCaption">The sub caption that appears directly under the caption. Do not pass null. Setting this to the empty string means there will be
		/// no sub caption.</param>
		/// <param name="allowExportToExcel">Set to true if you want an Export to Excel action component to appear. This will only work if the table consists of
		/// simple text (no controls).</param>
		/// <param name="tableActions">Table action components. This could be used to add a new customer or other entity to the table, for example.</param>
		/// <param name="fields">The table's fields. Do not pass an empty collection.</param>
		/// <param name="headItems">The table's head items.</param>
		/// <param name="firstDataFieldIndex">The index of the first data field.</param>
		/// <param name="etherealContent"></param>
		public static ColumnPrimaryTable<ItemIdType> CreateWithItemIdType<ItemIdType>(
			DisplaySetup displaySetup = null, EwfTableStyle style = EwfTableStyle.Standard, ElementClassSet classes = null, string postBackIdBase = "",
			string caption = "", string subCaption = "", bool allowExportToExcel = false, IReadOnlyCollection<ActionComponentSetup> tableActions = null,
			IReadOnlyCollection<EwfTableField> fields = null, IReadOnlyCollection<EwfTableItem<ItemIdType>> headItems = null, int firstDataFieldIndex = 0,
			IReadOnlyCollection<EtherealComponent> etherealContent = null ) =>
			new ColumnPrimaryTable<ItemIdType>(
				displaySetup,
				style,
				classes,
				postBackIdBase,
				caption,
				subCaption,
				allowExportToExcel,
				tableActions,
				fields,
				headItems,
				firstDataFieldIndex,
				etherealContent );

		private ColumnPrimaryTable(
			DisplaySetup displaySetup, EwfTableStyle style, ElementClassSet classes, string postBackIdBase, string caption, string subCaption,
			bool allowExportToExcel, IReadOnlyCollection<ActionComponentSetup> tableActions, IReadOnlyCollection<EwfTableField> fields,
			IReadOnlyCollection<EwfTableItem> headItems, int firstDataFieldIndex, IReadOnlyCollection<EtherealComponent> etherealContent ): base(
			displaySetup,
			style,
			classes,
			postBackIdBase,
			caption,
			subCaption,
			allowExportToExcel,
			tableActions,
			fields,
			headItems,
			firstDataFieldIndex,
			etherealContent ) {}
	}

	/// <summary>
	/// A column-primary table.
	/// </summary>
	public class ColumnPrimaryTable<ItemIdType>: FlowComponent {
		private readonly IReadOnlyCollection<DisplayableElement> outerChildren;
		private readonly PostBack exportToExcelPostBack;
		private readonly List<ColumnPrimaryItemGroup<ItemIdType>> itemGroups = new List<ColumnPrimaryItemGroup<ItemIdType>>();
		private bool? hasExplicitItemGroups;

		internal ColumnPrimaryTable(
			DisplaySetup displaySetup, EwfTableStyle style, ElementClassSet classes, string postBackIdBase, string caption, string subCaption,
			bool allowExportToExcel, IReadOnlyCollection<ActionComponentSetup> tableActions, IReadOnlyCollection<EwfTableField> fields,
			IReadOnlyCollection<EwfTableItem<ItemIdType>> headItems, int firstDataFieldIndex, IReadOnlyCollection<EtherealComponent> etherealContent ) {
			tableActions = tableActions ?? Enumerable.Empty<ActionComponentSetup>().Materialize();

			if( fields != null && !fields.Any() )
				throw new ApplicationException( "If fields are specified, there must be at least one of them." );

			headItems = headItems ?? Enumerable.Empty<EwfTableItem<ItemIdType>>().Materialize();

			var excelRowAdders = new List<Action<ExcelWorksheet>>();
			outerChildren = new DisplayableElement(
				tableContext => {
					var children = new List<FlowComponentOrNode>();

					children.AddRange( TableStatics.GetCaption( caption, subCaption ) );

					var itemSetupLists = new[] { headItems }.Concat( itemGroups.Select( i => i.Items ) )
						.Select( i => i.Select( j => j.Setup.FieldOrItemSetup ) )
						.Materialize();
					var allItemSetups = itemSetupLists.SelectMany( i => i ).ToImmutableArray();
					var columnWidthFactor = TableStatics.GetColumnWidthFactor( allItemSetups );
					foreach( var itemSetups in itemSetupLists.Where( i => i.Any() ) ) {
						children.Add(
							new ElementComponent(
								context => new ElementData(
									() => new ElementLocalData( "colgroup" ),
									children: itemSetups.Select( i => TableStatics.GetColElement( i, columnWidthFactor ) ).Materialize() ) ) );
					}

					var tHeadRows = new List<EwfTableItem>();
					var tableLevelGeneralActionList = TableStatics.GetGeneralActionList( allowExportToExcel ? exportToExcelPostBack : null, tableActions ).Materialize();
					if( tableLevelGeneralActionList.Any() )
						tHeadRows.Add(
							EwfTableItem.Create(
								new GenericFlowContainer( tableLevelGeneralActionList, classes: TableCssElementCreator.ItemLimitingAndGeneralActionContainerClass ).ToCell(
									new TableCellSetup( fieldSpan: allItemSetups.Length ) ) ) );
					// NOTE: Table-level item actions should go here.
					var groupHeadCells = itemGroups.Select( i => ( colSpan: i.Items.Count, content: i.GetHeadCellContent() ) ).Materialize();
					if( groupHeadCells.Any( i => i.content.Any() ) )
						tHeadRows.Add(
							EwfTableItem.Create(
								( headItems.Any() ? "".ToCell( setup: new TableCellSetup( fieldSpan: headItems.Count ) ).ToCollection() : Enumerable.Empty<EwfTableCell>() )
								.Concat( groupHeadCells.Select( i => i.content.ToCell( setup: new TableCellSetup( fieldSpan: i.colSpan ) ) ) )
								.Materialize() ) );
					// NOTE: The checkbox row should go here.
					if( tHeadRows.Any() ) {
						var cellPlaceholderListsForTHeadRows = TableStatics.BuildCellPlaceholderListsForItems( tHeadRows, allItemSetups.Length );
						children.Add(
							new ElementComponent(
								context => new ElementData(
									() => new ElementLocalData( "thead" ),
									children: TableStatics.BuildRows(
											cellPlaceholderListsForTHeadRows,
											tHeadRows.Select( i => i.Setup.FieldOrItemSetup ).ToImmutableArray(),
											null,
											Enumerable.Repeat( new EwfTableField().FieldOrItemSetup, allItemSetups.Length ).ToImmutableArray(),
											0,
											false )
										.Materialize() ) ) );
					}

					fields = TableStatics.GetFields( fields, headItems, itemGroups.SelectMany( i => i.Items ) );
					var cellPlaceholderListsForItems = TableStatics.BuildCellPlaceholderListsForItems(
						headItems.Concat( itemGroups.SelectMany( i => i.Items ) ).ToList(),
						fields.Count );

					// Pivot the cell placeholders from column primary into row primary format.
					var cellPlaceholderListsForRows = Enumerable.Range( 0, fields.Count )
						.Select( field => Enumerable.Range( 0, allItemSetups.Length ).Select( item => cellPlaceholderListsForItems[ item ][ field ] ).ToList() )
						.ToList();

					var headRows = TableStatics.BuildRows(
						cellPlaceholderListsForRows.Take( firstDataFieldIndex ).ToList(),
						fields.Select( i => i.FieldOrItemSetup ).ToImmutableArray(),
						null,
						allItemSetups,
						allItemSetups.Length,
						true );
					excelRowAdders.AddRange(
						cellPlaceholderListsForRows.Take( firstDataFieldIndex )
							.Select( i => TableStatics.GetExcelRowAdder( true, i.OfType<EwfTableCell>().Materialize() ) ) );

					var bodyRows = TableStatics.BuildRows(
						cellPlaceholderListsForRows.Skip( firstDataFieldIndex ).ToList(),
						fields.Select( i => i.FieldOrItemSetup ).ToImmutableArray(),
						false,
						allItemSetups,
						headItems.Count,
						true );
					excelRowAdders.AddRange(
						cellPlaceholderListsForRows.Skip( firstDataFieldIndex )
							.Select( i => TableStatics.GetExcelRowAdder( false, i.OfType<EwfTableCell>().Materialize() ) ) );

					// We can't easily put the head fields in thead because we don't have a way of verifying that cells don't cross between head and data fields.
					children.Add(
						new ElementComponent( context => new ElementData( () => new ElementLocalData( "tbody" ), children: headRows.Concat( bodyRows ).Materialize() ) ) );

					TableStatics.AssertAtLeastOneCellPerField( fields, cellPlaceholderListsForItems );

					return new DisplayableElementData(
						displaySetup,
						() => new DisplayableElementLocalData( "table" ),
						classes: TableStatics.GetClasses( style, classes ?? ElementClassSet.Empty ),
						children: children,
						etherealChildren: etherealContent );
				} ).ToCollection();

			exportToExcelPostBack = TableStatics.GetExportToExcelPostBack( postBackIdBase, caption, excelRowAdders );
		}

		/// <summary>
		/// Gets the Export to Excel post-back. This is convenient if you want to use the built-in export functionality, but from an external button.
		/// </summary>
		public PostBack ExportToExcelPostBack => exportToExcelPostBack;

		/// <summary>
		/// Adds items to the table. 
		/// </summary>
		public ColumnPrimaryTable<ItemIdType> AddItems( IReadOnlyCollection<EwfTableItem<ItemIdType>> items ) {
			if( hasExplicitItemGroups == true )
				throw new ApplicationException( "Item groups were previously added to the table. You cannot add both items and item groups." );
			hasExplicitItemGroups = false;

			var group = itemGroups.SingleOrDefault();
			if( group == null )
				itemGroups.Add( group = ColumnPrimaryItemGroup.CreateWithItemIdType<ItemIdType>( null ) );

			group.Items.AddRange( items );
			return this;
		}

		/// <summary>
		/// Adds item groups to the table.
		/// </summary>
		public ColumnPrimaryTable<ItemIdType> AddItemGroups( IReadOnlyCollection<ColumnPrimaryItemGroup<ItemIdType>> itemGroups ) {
			if( hasExplicitItemGroups == false )
				throw new ApplicationException( "Items were previously added to the table. You cannot add both items and item groups." );
			hasExplicitItemGroups = true;

			this.itemGroups.AddRange( itemGroups );
			return this;
		}

		IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() => outerChildren;
	}
}