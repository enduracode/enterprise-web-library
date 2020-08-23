using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using EnterpriseWebLibrary.IO;
using static MoreLinq.Extensions.EquiZipExtension;

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
		/// <param name="selectedItemActions">Table selected-item actions. Passing one or more of these will add a new row to the table containing a checkbox for
		/// each item with an ID. If you would like the table to support item-group-level selected-item actions, you must pass a collection here even if it is
		/// empty.</param>
		/// <param name="fields">The table's fields. Do not pass an empty collection.</param>
		/// <param name="headItems">The table's head items.</param>
		/// <param name="firstDataFieldIndex">The index of the first data field.</param>
		/// <param name="etherealContent"></param>
		public static ColumnPrimaryTable Create(
			DisplaySetup displaySetup = null, EwfTableStyle style = EwfTableStyle.Standard, ElementClassSet classes = null, string postBackIdBase = "",
			string caption = "", string subCaption = "", bool allowExportToExcel = false, IReadOnlyCollection<ActionComponentSetup> tableActions = null,
			IReadOnlyCollection<SelectedItemAction<int>> selectedItemActions = null, IReadOnlyCollection<EwfTableField> fields = null,
			IReadOnlyCollection<EwfTableItem> headItems = null, int firstDataFieldIndex = 0, IReadOnlyCollection<EtherealComponent> etherealContent = null ) =>
			new ColumnPrimaryTable(
				displaySetup,
				style,
				classes,
				postBackIdBase,
				caption,
				subCaption,
				allowExportToExcel,
				tableActions,
				selectedItemActions,
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
		/// <param name="selectedItemActions">Table selected-item actions. Passing one or more of these will add a new row to the table containing a checkbox for
		/// each item with an ID. If you would like the table to support item-group-level selected-item actions, you must pass a collection here even if it is
		/// empty.</param>
		/// <param name="fields">The table's fields. Do not pass an empty collection.</param>
		/// <param name="headItems">The table's head items.</param>
		/// <param name="firstDataFieldIndex">The index of the first data field.</param>
		/// <param name="etherealContent"></param>
		public static ColumnPrimaryTable<ItemIdType> CreateWithItemIdType<ItemIdType>(
			DisplaySetup displaySetup = null, EwfTableStyle style = EwfTableStyle.Standard, ElementClassSet classes = null, string postBackIdBase = "",
			string caption = "", string subCaption = "", bool allowExportToExcel = false, IReadOnlyCollection<ActionComponentSetup> tableActions = null,
			IReadOnlyCollection<SelectedItemAction<ItemIdType>> selectedItemActions = null, IReadOnlyCollection<EwfTableField> fields = null,
			IReadOnlyCollection<EwfTableItem> headItems = null, int firstDataFieldIndex = 0, IReadOnlyCollection<EtherealComponent> etherealContent = null ) =>
			new ColumnPrimaryTable<ItemIdType>(
				displaySetup,
				style,
				classes,
				postBackIdBase,
				caption,
				subCaption,
				allowExportToExcel,
				tableActions,
				selectedItemActions,
				fields,
				headItems,
				firstDataFieldIndex,
				etherealContent );

		private ColumnPrimaryTable(
			DisplaySetup displaySetup, EwfTableStyle style, ElementClassSet classes, string postBackIdBase, string caption, string subCaption,
			bool allowExportToExcel, IReadOnlyCollection<ActionComponentSetup> tableActions, IReadOnlyCollection<SelectedItemAction<int>> selectedItemActions,
			IReadOnlyCollection<EwfTableField> fields, IReadOnlyCollection<EwfTableItem> headItems, int firstDataFieldIndex,
			IReadOnlyCollection<EtherealComponent> etherealContent ): base(
			displaySetup,
			style,
			classes,
			postBackIdBase,
			caption,
			subCaption,
			allowExportToExcel,
			tableActions,
			selectedItemActions,
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
		private readonly string postBackIdBase;
		private readonly PostBack exportToExcelPostBack;
		private readonly IReadOnlyCollection<SelectedItemAction<ItemIdType>> selectedItemActions;
		private readonly TableSelectedItemData<ItemIdType> selectedItemData = new TableSelectedItemData<ItemIdType>();
		private readonly List<ColumnPrimaryItemGroup<ItemIdType>> itemGroups = new List<ColumnPrimaryItemGroup<ItemIdType>>();
		private bool? hasExplicitItemGroups;

		internal ColumnPrimaryTable(
			DisplaySetup displaySetup, EwfTableStyle style, ElementClassSet classes, string postBackIdBase, string caption, string subCaption,
			bool allowExportToExcel, IReadOnlyCollection<ActionComponentSetup> tableActions, IReadOnlyCollection<SelectedItemAction<ItemIdType>> selectedItemActions,
			IReadOnlyCollection<EwfTableField> fields, IReadOnlyCollection<EwfTableItem> headItems, int firstDataFieldIndex,
			IReadOnlyCollection<EtherealComponent> etherealContent ) {
			tableActions = tableActions ?? Enumerable.Empty<ActionComponentSetup>().Materialize();

			if( fields != null && !fields.Any() )
				throw new ApplicationException( "If fields are specified, there must be at least one of them." );

			headItems = headItems ?? Enumerable.Empty<EwfTableItem>().Materialize();

			var excelRowAdders = new List<Action<ExcelWorksheet>>();
			outerChildren = new DisplayableElement(
				tableContext => {
					if( selectedItemData.Buttons == null )
						TableStatics.AddCheckboxes(
							postBackIdBase,
							selectedItemActions,
							selectedItemData,
							itemGroups.Select( i => ( i.SelectedItemActions, i.Items.ToFunctions() ) ),
							null,
							Enumerable.Empty<DataModification>().Materialize() );

					TableStatics.AssertItemIdsUnique( itemGroups.SelectMany( i => i.Items ) );

					var children = new List<FlowComponentOrNode>();

					children.AddRange( TableStatics.GetCaption( caption, subCaption ) );

					var itemSetupLists = new[] { headItems.Select( i => i.Setup.FieldOrItemSetup ).Materialize() }
						.Concat( itemGroups.Select( i => i.Items.Select( j => j.Setup.FieldOrItemSetup ).Materialize() ) )
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
					if( selectedItemData.ItemGroupData != null )
						tHeadRows.Add(
							EwfTableItem.Create(
								TableStatics.GetItemSelectionAndActionComponents(
										"$( this ).closest( 'thead' ).children( ':last-child' ).children()",
										selectedItemData.Buttons,
										selectedItemData.Validation )
									.ToCell( new TableCellSetup( fieldSpan: allItemSetups.Length ) ) ) );
					var groupHeadCells = itemGroups.Select( i => ( colSpan: i.Items.Count, content: i.GetHeadCellContent() ) ).Materialize();
					if( groupHeadCells.Any( i => i.content.Any() ) )
						tHeadRows.Add(
							EwfTableItem.Create(
								( headItems.Any() ? "".ToCell( setup: new TableCellSetup( fieldSpan: headItems.Count ) ).ToCollection() : Enumerable.Empty<EwfTableCell>() )
								.Concat( groupHeadCells.Select( i => i.content.ToCell( setup: new TableCellSetup( fieldSpan: i.colSpan ) ) ) )
								.Materialize() ) );
					if( hasExplicitItemGroups == true && selectedItemData.ItemGroupData != null && selectedItemData.ItemGroupData.Any( i => i.HasValue ) )
						tHeadRows.Add(
							EwfTableItem.Create(
								( headItems.Any() ? "".ToCell( setup: new TableCellSetup( fieldSpan: headItems.Count ) ).ToCollection() : Enumerable.Empty<EwfTableCell>() )
								.Concat(
									itemGroups.EquiZip(
										selectedItemData.ItemGroupData,
										( group, groupSelectedItemData ) => ( groupSelectedItemData.HasValue
											                                      ? TableStatics.GetItemSelectionAndActionComponents(
												                                      "$( this ).closest( 'thead' ).children( ':last-child' ).children()",
												                                      groupSelectedItemData.Value.buttons,
												                                      groupSelectedItemData.Value.validation )
											                                      : null ).ToCell( setup: new TableCellSetup( fieldSpan: group.Items.Count ) ) ) )
								.Materialize() ) );
					if( selectedItemData.ItemGroupData != null )
						tHeadRows.Add(
							EwfTableItem.Create(
								( headItems.Any() ? "".ToCell( setup: new TableCellSetup( fieldSpan: headItems.Count ) ).ToCollection() : Enumerable.Empty<EwfTableCell>() )
								.Concat(
									itemGroups.EquiZip(
											selectedItemData.ItemGroupData,
											( group, groupSelectedItemData ) => groupSelectedItemData.HasValue
												                                    ? group.Items.EquiZip(
													                                    groupSelectedItemData.Value.checkboxes,
													                                    ( item, checkbox ) => item.Setup.Id != null ? checkbox : null )
												                                    : Enumerable.Repeat( (PhrasingComponent)null, group.Items.Count ) )
										.SelectMany( i => i )
										.Select( i => i.ToCell( setup: new TableCellSetup( containsActivatableElements: i != null ) ) ) )
								.Materialize() ) );
					if( tHeadRows.Any() ) {
						var cellPlaceholderListsForTHeadRows = TableStatics.BuildCellPlaceholderListsForItems(
							tHeadRows.Select( i => i.Cells ).Materialize(),
							allItemSetups.Length );
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
						headItems.Select( i => i.Cells ).Concat( itemGroups.SelectMany( i => i.Items ).Select( i => i.Cells ) ).Materialize(),
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

					TableStatics.AssertAtLeastOneCellPerField( fields.Count, cellPlaceholderListsForItems );

					return new DisplayableElementData(
						displaySetup,
						() => new DisplayableElementLocalData( "table" ),
						classes: TableStatics.GetClasses( style, classes ?? ElementClassSet.Empty ),
						children: children,
						etherealChildren: etherealContent );
				} ).ToCollection();

			this.postBackIdBase = postBackIdBase;
			exportToExcelPostBack = TableStatics.GetExportToExcelPostBack( postBackIdBase, caption, excelRowAdders );
			this.selectedItemActions = selectedItemActions;
		}

		/// <summary>
		/// Gets the Export to Excel post-back. This is convenient if you want to use the built-in export functionality, but from an external button.
		/// </summary>
		public PostBack ExportToExcelPostBack => exportToExcelPostBack;

		/// <summary>
		/// Adds items to the table.
		/// 
		/// You can pass EwfTableItem wherever EwfTableItem&lt;int&gt; is expected.
		/// </summary>
		public ColumnPrimaryTable<ItemIdType> AddItems( IReadOnlyCollection<EwfTableItem<ItemIdType>> items ) {
			if( selectedItemData.Buttons != null )
				throw new ApplicationException( "You cannot modify the table after checkboxes have been added." );

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
		/// 
		/// You can pass ColumnPrimaryItemGroup wherever ColumnPrimaryItemGroup&lt;int&gt; is expected.
		/// </summary>
		public ColumnPrimaryTable<ItemIdType> AddItemGroups( IReadOnlyCollection<ColumnPrimaryItemGroup<ItemIdType>> itemGroups ) {
			if( selectedItemData.Buttons != null )
				throw new ApplicationException( "You cannot modify the table after checkboxes have been added." );

			if( hasExplicitItemGroups == false )
				throw new ApplicationException( "Items were previously added to the table. You cannot add both items and item groups." );
			hasExplicitItemGroups = true;

			if( itemGroups.Any( i => i.SelectedItemActions.Any() ) && selectedItemActions == null )
				throw new ApplicationException( "Selected-item actions are disabled." );

			this.itemGroups.AddRange( itemGroups );
			return this;
		}

		/// <summary>
		/// Adds a new row to the table containing a checkbox for each item with an ID. Validation will put the selected-item IDs in the specified <see cref="DataValue{T}"/>.
		/// </summary>
		/// <param name="selectedItemIds">Do not pass null.</param>
		public ColumnPrimaryTable<ItemIdType> AddCheckboxes( DataValue<IReadOnlyCollection<ItemIdType>> selectedItemIds ) {
			TableStatics.AddCheckboxes(
				postBackIdBase,
				selectedItemActions,
				selectedItemData,
				itemGroups.Select( i => ( i.SelectedItemActions, i.Items.ToFunctions() ) ),
				selectedItemIds,
				FormState.Current.DataModifications );
			return this;
		}

		IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() => outerChildren;
	}
}