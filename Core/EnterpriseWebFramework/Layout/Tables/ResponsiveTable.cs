#nullable disable
using System.Collections.Immutable;
using Humanizer;
using JetBrains.Annotations;
using StackExchange.Profiling;
using Tewl.IO;
using static MoreLinq.Extensions.EquiZipExtension;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// A table.
/// </summary>
[ PublicAPI ]
public class ResponsiveTable: ResponsiveTable<int> {
	/// <summary>
	/// Creates a table.
	/// </summary>
	/// <param name="displaySetup"></param>
	/// <param name="style">The table’s style.</param>
	/// <param name="classes">The classes on the table.</param>
	/// <param name="idBase">The base for the table’s post-back and component-state-item IDs. This must be unique on the page, meaning that if the page includes
	/// multiple tables, you must pass a different value for each. Do not pass null.</param>
	/// <param name="caption">The caption that appears above the table. Do not pass null. Setting this to the empty string means the table will have no caption.
	/// </param>
	/// <param name="subCaption">The sub caption that appears directly under the caption. Do not pass null. Setting this to the empty string means there will be
	/// no sub caption.</param>
	/// <param name="allowExportToExcel">Set to true if you want an Export to Excel action component to appear. This will only work if the table consists of
	/// simple text (no controls).</param>
	/// <param name="tableActions">Table action components. This could be used to add a new customer or other entity to the table, for example. Any hyperlink with
	/// a destination to which the user cannot navigate (due to authorization logic) will be automatically hidden by the framework.</param>
	/// <param name="selectedItemActions">Table selected-item actions. Passing one or more of these will add a new column to the table containing a checkbox for
	/// each item with an ID. If you would like the table to support item-group-level selected-item actions, you must pass a collection here even if it is
	/// empty.</param>
	/// <param name="fields">The table’s fields. Do not pass an empty collection.</param>
	/// <param name="headItems">The table’s head items.</param>
	/// <param name="defaultItemLimit">The maximum number of result items that will be shown. Default is DataRowLimit.Unlimited. A default item limit of
	/// anything other than Unlimited will cause the table to show a control allowing the user to select how many results they want to see, as well as an
	/// indicator of the total number of results that would be shown if there was no limit.</param>
	/// <param name="enableItemReordering">Pass true to add a column to the table containing controls for reordering items. Every item must have a rank ID, with
	/// the exception of item groups in which none of the items have rank IDs.</param>
	/// <param name="disableEmptyFieldDetection">Set to true if you want to disable the "at least one cell per field" assertion. Use with caution.</param>
	/// <param name="tailUpdateRegions">The tail update regions for the table, which will operate on the item level if you add items, or the item-group level if
	/// you add item groups.</param>
	/// <param name="etherealContent"></param>
	public static ResponsiveTable Create(
		DisplaySetup displaySetup = null, EwfTableStyle style = EwfTableStyle.Standard, ElementClassSet classes = null, string idBase = "", string caption = "",
		string subCaption = "", bool allowExportToExcel = false, IReadOnlyCollection<ActionComponentSetup> tableActions = null,
		IReadOnlyCollection<SelectedItemAction<int>> selectedItemActions = null, IReadOnlyCollection<EwfTableField> fields = null,
		IReadOnlyCollection<EwfTableItem> headItems = null, DataRowLimit defaultItemLimit = DataRowLimit.Unlimited, bool enableItemReordering = false,
		bool disableEmptyFieldDetection = false, IReadOnlyCollection<TailUpdateRegion> tailUpdateRegions = null,
		IReadOnlyCollection<EtherealComponent> etherealContent = null ) =>
		new(
			displaySetup,
			style,
			classes,
			idBase,
			caption,
			subCaption,
			allowExportToExcel,
			tableActions,
			selectedItemActions,
			fields,
			headItems,
			defaultItemLimit,
			enableItemReordering,
			disableEmptyFieldDetection,
			tailUpdateRegions,
			etherealContent );

	/// <summary>
	/// Creates a table with a specified item ID type.
	/// </summary>
	/// <param name="displaySetup"></param>
	/// <param name="style">The table’s style.</param>
	/// <param name="classes">The classes on the table.</param>
	/// <param name="idBase">The base for the table’s post-back and component-state-item IDs. This must be unique on the page, meaning that if the page includes
	/// multiple tables, you must pass a different value for each. Do not pass null.</param>
	/// <param name="caption">The caption that appears above the table. Do not pass null. Setting this to the empty string means the table will have no caption.
	/// </param>
	/// <param name="subCaption">The sub caption that appears directly under the caption. Do not pass null. Setting this to the empty string means there will be
	/// no sub caption.</param>
	/// <param name="allowExportToExcel">Set to true if you want an Export to Excel action component to appear. This will only work if the table consists of
	/// simple text (no controls).</param>
	/// <param name="tableActions">Table action components. This could be used to add a new customer or other entity to the table, for example. Any hyperlink with
	/// a destination to which the user cannot navigate (due to authorization logic) will be automatically hidden by the framework.</param>
	/// <param name="selectedItemActions">Table selected-item actions. Passing one or more of these will add a new column to the table containing a checkbox for
	/// each item with an ID. If you would like the table to support item-group-level selected-item actions, you must pass a collection here even if it is
	/// empty.</param>
	/// <param name="fields">The table’s fields. Do not pass an empty collection.</param>
	/// <param name="headItems">The table’s head items.</param>
	/// <param name="defaultItemLimit">The maximum number of result items that will be shown. Default is DataRowLimit.Unlimited. A default item limit of
	/// anything other than Unlimited will cause the table to show a control allowing the user to select how many results they want to see, as well as an
	/// indicator of the total number of results that would be shown if there was no limit.</param>
	/// <param name="enableItemReordering">Pass true to add a column to the table containing controls for reordering items. Every item must have a rank ID, with
	/// the exception of item groups in which none of the items have rank IDs.</param>
	/// <param name="disableEmptyFieldDetection">Set to true if you want to disable the "at least one cell per field" assertion. Use with caution.</param>
	/// <param name="tailUpdateRegions">The tail update regions for the table, which will operate on the item level if you add items, or the item-group level if
	/// you add item groups.</param>
	/// <param name="etherealContent"></param>
	public static ResponsiveTable<ItemIdType> CreateWithItemIdType<ItemIdType>(
		DisplaySetup displaySetup = null, EwfTableStyle style = EwfTableStyle.Standard, ElementClassSet classes = null, string idBase = "", string caption = "",
		string subCaption = "", bool allowExportToExcel = false, IReadOnlyCollection<ActionComponentSetup> tableActions = null,
		IReadOnlyCollection<SelectedItemAction<ItemIdType>> selectedItemActions = null, IReadOnlyCollection<EwfTableField> fields = null,
		IReadOnlyCollection<EwfTableItem> headItems = null, DataRowLimit defaultItemLimit = DataRowLimit.Unlimited, bool enableItemReordering = false,
		bool disableEmptyFieldDetection = false, IReadOnlyCollection<TailUpdateRegion> tailUpdateRegions = null,
		IReadOnlyCollection<EtherealComponent> etherealContent = null ) =>
		new(
			displaySetup,
			style,
			classes,
			idBase,
			caption,
			subCaption,
			allowExportToExcel,
			tableActions,
			selectedItemActions,
			fields,
			headItems,
			defaultItemLimit,
			enableItemReordering,
			disableEmptyFieldDetection,
			tailUpdateRegions,
			etherealContent );

	private ResponsiveTable(
		DisplaySetup displaySetup, EwfTableStyle style, ElementClassSet classes, string postBackIdBase, string caption, string subCaption, bool allowExportToExcel,
		IReadOnlyCollection<ActionComponentSetup> tableActions, IReadOnlyCollection<SelectedItemAction<int>> selectedItemActions,
		IReadOnlyCollection<EwfTableField> fields, IReadOnlyCollection<EwfTableItem> headItems, DataRowLimit defaultItemLimit, bool enableItemReordering,
		bool disableEmptyFieldDetection, IReadOnlyCollection<TailUpdateRegion> tailUpdateRegions, IReadOnlyCollection<EtherealComponent> etherealContent ): base(
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
		defaultItemLimit,
		enableItemReordering,
		disableEmptyFieldDetection,
		tailUpdateRegions,
		etherealContent ) {}
}

/// <summary>
/// A table.
/// </summary>
[ PublicAPI ]
public class ResponsiveTable<ItemIdType>: FlowComponent {
	private readonly IReadOnlyCollection<DisplayableElement> outerChildren;
	private readonly string idBase;
	private readonly PostBack exportToExcelPostBack;
	private readonly IReadOnlyCollection<SelectedItemAction<ItemIdType>> selectedItemActions;
	private readonly TableSelectedItemData<ItemIdType> selectedItemData = new();
	private readonly ComponentStateItem<int> itemLimit;
	private readonly List<EwfTableItemGroup<ItemIdType>> itemGroups = new();
	private bool? hasExplicitItemGroups;
	private IReadOnlyCollection<TailUpdateRegion> tailUpdateRegions;

	internal ResponsiveTable(
		DisplaySetup displaySetup, EwfTableStyle style, ElementClassSet classes, string idBase, string caption, string subCaption, bool allowExportToExcel,
		IReadOnlyCollection<ActionComponentSetup> tableActions, IReadOnlyCollection<SelectedItemAction<ItemIdType>> selectedItemActions,
		IReadOnlyCollection<EwfTableField> fields, IReadOnlyCollection<EwfTableItem> headItems, DataRowLimit defaultItemLimit, bool enableItemReordering,
		bool disableEmptyFieldDetection, IReadOnlyCollection<TailUpdateRegion> tailUpdateRegions, IReadOnlyCollection<EtherealComponent> etherealContent ) {
		idBase = PostBack.GetCompositeId( idBase, "ewfTable" );
		tableActions ??= Enumerable.Empty<ActionComponentSetup>().Materialize();

		if( fields != null && !fields.Any() )
			throw new ApplicationException( "If fields are specified, there must be at least one of them." );

		headItems ??= Enumerable.Empty<EwfTableItem>().Materialize();
		tailUpdateRegions ??= Enumerable.Empty<TailUpdateRegion>().Materialize();

		var dataModifications = FormState.Current.DataModifications;

		var excelRowAdders = new List<Action<ExcelWorksheet>>();
		outerChildren = new DisplayableElement(
			context => {
				if( selectedItemData.Buttons == null )
					TableStatics.AddCheckboxes(
						idBase,
						selectedItemActions,
						selectedItemData,
						itemGroups.Select( group => ( group.SelectedItemActions, group.Items.Select( i => new Func<EwfTableItem<ItemIdType>>( () => i.Value ) ) ) ),
						null,
						Enumerable.Empty<DataModification>().Materialize() );

				var children = new List<FlowComponentOrNode>();
				using( MiniProfiler.Current.Step( "EWF - Load table data" ) )
					FormState.ExecuteWithDataModificationsAndDefaultAction(
						dataModifications,
						() => {
							children.AddRange( TableStatics.GetCaption( caption, subCaption ) );

							var visibleItemGroupsAndItems = new List<( EwfTableItemGroup<ItemIdType>, IReadOnlyList<EwfTableItem<ItemIdType>> )>();
							foreach( var itemGroup in itemGroups ) {
								var limit = defaultItemLimit != DataRowLimit.Unlimited ? itemLimit.Value.Value : (int)DataRowLimit.Unlimited;
								var visibleItems = itemGroup.Items.Take( limit - visibleItemGroupsAndItems.Sum( i => i.Item2.Count ) ).Select( i => i.Value );
								visibleItemGroupsAndItems.Add( ( itemGroup, visibleItems.ToImmutableArray() ) );
								if( visibleItemGroupsAndItems.Sum( i => i.Item2.Count ) == limit )
									break;
							}

							TableStatics.AssertItemIdsUnique( visibleItemGroupsAndItems.SelectMany( i => i.Item2 ) );

							fields = TableStatics.GetFields( fields, headItems, visibleItemGroupsAndItems.SelectMany( i => i.Item2 ) );
							if( !fields.Any() )
								fields = new EwfTableField().ToCollection();
							else {
								if( selectedItemData.ItemGroupData != null )
									fields = fields.Prepend( new EwfTableField( size: 2.5m.ToEm() ) ).Materialize();
								if( enableItemReordering )
									fields = fields.Append( new EwfTableField( size: 5.ToEm(), textAlignment: TextAlignment.Center ) ).Materialize();
							}

							var allVisibleItems = new List<IReadOnlyCollection<EwfTableCell>>();

							var itemLimitingUpdateRegionSet = new UpdateRegionSet();
							var itemLimitingAndGeneralActionComponents =
								( defaultItemLimit != DataRowLimit.Unlimited
									  ? getItemLimitingControlContainer( idBase, itemLimit.Value, itemLimitingUpdateRegionSet, this.tailUpdateRegions ).ToCollection()
									  : Enumerable.Empty<FlowComponent>() )
								.Concat( TableStatics.GetGeneralActionList( allowExportToExcel ? exportToExcelPostBack : null, tableActions ) )
								.Materialize();
							var headRows = buildRows(
									( itemLimitingAndGeneralActionComponents.Any()
										  ? EwfTableItem.Create(
												  new GenericFlowContainer(
													  itemLimitingAndGeneralActionComponents,
													  classes: TableCssElementCreator.ItemLimitingAndGeneralActionContainerClass ).ToCell(
													  new TableCellSetup( fieldSpan: fields.Count ) ) )
											  .ToCollection()
										  : Enumerable.Empty<EwfTableItem>() )
									.Concat(
										selectedItemData.ItemGroupData != null
											? EwfTableItem.Create(
													TableStatics.GetItemSelectionAndActionComponents(
															"$( this ).closest( 'table' ).children( 'tbody' ).children().children( ':first-child' )",
															selectedItemData.Buttons,
															selectedItemData.Validation )
														.ToCell( new TableCellSetup( fieldSpan: fields.Count ) ) )
												.ToCollection()
											: Enumerable.Empty<EwfTableItem>() )
									.Materialize(),
									Enumerable.Repeat( new EwfTableField(), fields.Count ).Materialize(),
									null,
									null,
									null,
									false,
									null )
								.Concat(
									buildRows(
										headItems,
										fields,
										selectedItemData.ItemGroupData == null ? null : Enumerable.Repeat( (PhrasingComponent)null, headItems.Count ),
										!enableItemReordering ? null : Enumerable.Repeat( (IReadOnlyCollection<PhrasingComponent>)null, headItems.Count ),
										null,
										true,
										allVisibleItems ) )
								.Materialize();
							if( headRows.Any() )
								children.Add( new ElementComponent( _ => new ElementData( () => new ElementLocalData( "thead" ), children: headRows ) ) );
							excelRowAdders.AddRange( headItems.Select( i => TableStatics.GetExcelRowAdder( true, i.Cells ) ) );

							var bodyRowGroupsAndRows = new List<Tuple<FlowComponent, IReadOnlyCollection<FlowComponent>>>();
							var updateRegionSetListsAndStaticRowGroupCounts = new List<Tuple<IReadOnlyCollection<UpdateRegionSet>, int>>();
							for( var visibleGroupIndex = 0; visibleGroupIndex < visibleItemGroupsAndItems.Count; visibleGroupIndex += 1 ) {
								var groupAndItems = visibleItemGroupsAndItems[ visibleGroupIndex ];
								var groupSelectedItemData = selectedItemData.ItemGroupData?[ visibleGroupIndex ];
								var groupBodyRows = buildRows(
										groupAndItems.Item2,
										fields,
										selectedItemData.ItemGroupData == null ? null :
										!groupSelectedItemData.HasValue ? Enumerable.Repeat( (PhrasingComponent)null, groupAndItems.Item2.Count ) :
										groupSelectedItemData.Value.checkboxes.Take( groupAndItems.Item2.Count )
											.EquiZip( groupAndItems.Item2, ( checkbox, item ) => item.Setup.Id != null ? checkbox : null ),
										TableStatics.GetReorderingControls( idBase, false, enableItemReordering, hasExplicitItemGroups.Value, groupAndItems.Item2 ),
										null,
										false,
										allVisibleItems )
									.Materialize();
								var cachedVisibleGroupIndex = visibleGroupIndex;
								FlowComponent rowGroup = new ElementComponent(
									_ => new ElementData(
										() => new ElementLocalData( "tbody" ),
										children:
										buildRows(
												groupAndItems.Item1.GetHeadItem( fields.Count ),
												Enumerable.Repeat( new EwfTableField(), fields.Count ).Materialize(),
												null,
												null,
												null,
												true,
												null )
											.Concat(
												buildRows(
													hasExplicitItemGroups == true && groupSelectedItemData.HasValue
														? EwfTableItem.Create(
																TableStatics.GetItemSelectionAndActionComponents(
																		"$( this ).closest( 'tbody' ).children().children( ':first-child' )",
																		groupSelectedItemData.Value.buttons,
																		groupSelectedItemData.Value.validation )
																	.ToCell( new TableCellSetup( fieldSpan: fields.Count ) ) )
															.ToCollection()
														: Enumerable.Empty<EwfTableItem>().Materialize(),
													Enumerable.Repeat( new EwfTableField(), fields.Count ).Materialize(),
													null,
													null,
													null,
													false,
													null ) )
											.Append<FlowComponentOrNode>(
												new IdentifiedFlowComponent(
													() => new IdentifiedComponentData<FlowComponentOrNode>(
														"",
														new UpdateRegionLinker(
															"tail",
															from region in hasExplicitItemGroups.Value
																               ? groupAndItems.Item1.RemainingData.Value.TailUpdateRegions
																               : groupAndItems.Item1.GetTailUpdateRegionsNotIncludingAllItems()
															let staticRowCount = itemGroups[ cachedVisibleGroupIndex ].Items.Count - region.UpdatingItemCount
															select new PreModificationUpdateRegion( region.Sets, () => groupBodyRows.Skip( staticRowCount ), staticRowCount.ToString ),
															arg => groupBodyRows.Skip( int.Parse( arg ) ) ).ToCollection(),
														new ErrorSourceSet(),
														_ => groupBodyRows ) ) )
											.Materialize() ) );
								bodyRowGroupsAndRows.Add( Tuple.Create( rowGroup, groupBodyRows ) );
								excelRowAdders.AddRange( groupAndItems.Item2.Select( i => TableStatics.GetExcelRowAdder( false, i.Cells ) ) );

								// If item limiting is enabled, include all subsequent item groups in tail update regions since any number of items could be appended.
								if( defaultItemLimit != DataRowLimit.Unlimited )
									updateRegionSetListsAndStaticRowGroupCounts.Add(
										Tuple.Create( groupAndItems.Item1.RemainingData.Value.TailUpdateRegions.SelectMany( i => i.Sets ).Materialize(), visibleGroupIndex + 1 ) );
							}
							var linkers = new List<UpdateRegionLinker>();
							children.Add(
								new IdentifiedFlowComponent(
									() => new IdentifiedComponentData<FlowComponentOrNode>(
										"",
										linkers,
										new ErrorSourceSet(),
										_ => bodyRowGroupsAndRows.Select( i => i.Item1 ) ) ) );

							if( defaultItemLimit != DataRowLimit.Unlimited ) {
								var oldItemLimit = itemLimit.Value.Value;
								var lowerItemLimit = new Lazy<int>( () => Math.Min( oldItemLimit, itemLimit.Value.Value ) );

								var itemLimitingTailUpdateRegionComponentGetter = new Func<int, IEnumerable<FlowComponent>>(
									staticItemCount => {
										var rowCount = 0;
										for( var groupIndex = 0; groupIndex < bodyRowGroupsAndRows.Count; groupIndex += 1 ) {
											var rows = bodyRowGroupsAndRows[ groupIndex ].Item2;
											rowCount += rows.Count;
											if( rowCount < staticItemCount )
												continue;
											return rows.Skip( rows.Count - ( rowCount - staticItemCount ) )
												.Concat( bodyRowGroupsAndRows.Skip( groupIndex + 1 ).Select( i => i.Item1 ) );
										}
										return Enumerable.Empty<FlowComponent>();
									} );

								linkers.Add(
									new UpdateRegionLinker(
										"itemLimitingTail",
										new PreModificationUpdateRegion(
											itemLimitingUpdateRegionSet.ToCollection(),
											() => itemLimitingTailUpdateRegionComponentGetter( lowerItemLimit.Value ),
											() => lowerItemLimit.Value.ToString() ).ToCollection(),
										arg => itemLimitingTailUpdateRegionComponentGetter( int.Parse( arg ) ) ) );
							}

							linkers.Add(
								new UpdateRegionLinker(
									"tail",
									from region in hasExplicitItemGroups == false
										               ? visibleItemGroupsAndItems.Single()
											               .Item1.GetTailUpdateRegionsIncludingAllItems()
											               .Select( i => new { sets = i.Sets, staticRowGroupCount = 0 } )
										               : this.tailUpdateRegions.Select( i => new { sets = i.Sets, staticRowGroupCount = itemGroups.Count - i.UpdatingItemCount } )
											               .Concat( updateRegionSetListsAndStaticRowGroupCounts.Select( i => new { sets = i.Item1, staticRowGroupCount = i.Item2 } ) )
									select new PreModificationUpdateRegion(
										region.sets,
										() => bodyRowGroupsAndRows.Skip( region.staticRowGroupCount ).Select( i => i.Item1 ),
										region.staticRowGroupCount.ToString ),
									arg => bodyRowGroupsAndRows.Skip( int.Parse( arg ) ).Select( i => i.Item1 ) ) );

							var itemCount = itemGroups.Sum( i => i.Items.Count );
							var itemLimitingRowGroup = new List<FlowComponent>();
							if( defaultItemLimit != DataRowLimit.Unlimited && itemLimit.Value.Value < itemCount ) {
								var nextLimit = EnumTools.GetValues<DataRowLimit>().First( i => i > (DataRowLimit)itemLimit.Value.Value );
								var itemIncrementCount = Math.Min( (int)nextLimit, itemCount ) - itemLimit.Value.Value;
								var button = new EwfButton(
									new StandardButtonStyle( "Show " + itemIncrementCount + " more item" + ( itemIncrementCount != 1 ? "s" : "" ) ),
									behavior: new PostBackBehavior(
										postBack: PostBack.CreateIntermediate(
											itemLimitingUpdateRegionSet.ToCollection(),
											id: PostBack.GetCompositeId( idBase, "showMore" ),
											modificationMethod: () => itemLimit.Value.Value = (int)nextLimit ) ) );
								var item = EwfTableItem.Create( button.ToCollection().ToCell( new TableCellSetup( fieldSpan: fields.Count ) ) );
								itemLimitingRowGroup.Add(
									new ElementComponent(
										_ => new ElementData(
											() => new ElementLocalData( "tbody" ),
											children: buildRows(
													item.ToCollection(),
													Enumerable.Repeat( new EwfTableField(), fields.Count ).Materialize(),
													null,
													null,
													null,
													false,
													null )
												.Materialize() ) ) );
							}
							children.Add(
								new FlowIdContainer(
									itemLimitingRowGroup,
									updateRegionSets: itemLimitingUpdateRegionSet.ToCollection()
										.Concat(
											itemGroups.SelectMany( i => i.RemainingData.Value.TailUpdateRegions )
												.Materialize()
												.Concat( this.tailUpdateRegions )
												.SelectMany( i => i.Sets ) ) ) );

							// Assert that every visible item in the table has the same number of cells and store a data structure for below.
							var fieldCount = fields.Count - ( selectedItemData.ItemGroupData != null ? 1 : 0 ) - ( enableItemReordering ? 1 : 0 );
							var cellPlaceholderListsForItems = TableStatics.BuildCellPlaceholderListsForItems( allVisibleItems, fieldCount );

							if( !disableEmptyFieldDetection )
								TableStatics.AssertAtLeastOneCellPerField( fieldCount, cellPlaceholderListsForItems );
						} );
				return new DisplayableElementData(
					displaySetup,
					() => new DisplayableElementLocalData(
						"table",
						focusDependentData: new DisplayableElementFocusDependentData(
							includeIdAttribute: true,
							jsInitStatements:
							$"$( '#{context.Id}' ).DataTable( {{ columns: [ {getDataTablesColumnSpecifications( fields )} ], info: false, order: [], paging: false, responsive: true, searching: false }} );" ) ),
					classes: TableStatics.GetClasses(
						style,
						new ElementClass( "ewfResponsive" ).Add( new ElementClass( "compact" ) ) // see https://datatables.net/manual/styling/classes
							.Add( classes ?? ElementClassSet.Empty ) ),
					children: children,
					etherealChildren: ( defaultItemLimit != DataRowLimit.Unlimited ? itemLimit.ToCollection() : Enumerable.Empty<EtherealComponent>() )
					.Concat( etherealContent ?? Enumerable.Empty<EtherealComponent>() )
					.Materialize() );
			} ).ToCollection();

		this.idBase = idBase;
		exportToExcelPostBack = TableStatics.GetExportToExcelPostBack( idBase, caption, excelRowAdders );
		this.selectedItemActions = selectedItemActions;
		if( defaultItemLimit != DataRowLimit.Unlimited )
			itemLimit = ComponentStateItem.Create(
				PostBack.GetCompositeId( idBase, "itemLimit" ),
				(int)defaultItemLimit,
				value => Enum.IsDefined( typeof( DataRowLimit ), value ),
				false );
		this.tailUpdateRegions = tailUpdateRegions;
	}

	/// <summary>
	/// Gets the Export to Excel post-back. This is convenient if you want to use the built-in export functionality, but from an external button.
	/// </summary>
	public PostBack ExportToExcelPostBack => exportToExcelPostBack;

	/// <summary>
	/// Adds all of the given data to the table by enumerating the data and translating each item into an EwfTableItem using the given itemSelector. If
	/// enumerating the data is expensive, this call will be slow. The data must be enumerated so the table can show the total number of items.
	/// 
	/// You can pass EwfTableItem wherever EwfTableItem&lt;int&gt; is expected.
	/// </summary>
	/// <param name="data"></param>
	/// <param name="itemSelector"></param>
	/// <param name="createItemsImmediately">Pass true to create the table items during this call rather than deferring their creation. Use if you are directly
	/// or indirectly creating validations inside the item selector that will be added to an outside data modification. Deferring item creation would likely
	/// cause your validations to execute in the wrong order or be skipped.</param>
	public ResponsiveTable<ItemIdType> AddData<T>( IEnumerable<T> data, Func<T, EwfTableItem<ItemIdType>> itemSelector, bool createItemsImmediately = false ) {
		if( createItemsImmediately )
			foreach( var i in data )
				AddItem( itemSelector( i ) );
		else
			foreach( var i in data )
				AddItem( () => itemSelector( i ) );
		return this;
	}

	/// <summary>
	/// Adds items to the table based on the current item limit.
	/// 
	/// You can pass EwfTableItem wherever EwfTableItem&lt;int&gt; is expected.
	/// </summary>
	/// <param name="itemGetter">A function that takes the remaining number of items that can be shown based on the current limit and returns the items to add
	/// along with the total number of items that could be added if there was no limit.</param>
	public ResponsiveTable<ItemIdType> AddLimitedItems( Func<int, ( IEnumerable<EwfTableItem<ItemIdType>> items, int totalItemCount )> itemGetter ) {
		var itemsAndCount = itemGetter( ( itemLimit != null ? itemLimit.Value.Value : (int)DataRowLimit.Unlimited ) - itemGroups.Sum( i => i.Items.Count ) );

		var count = 0;
		foreach( var i in itemsAndCount.items ) {
			AddItem( i );
			count += 1;
		}

		if( itemsAndCount.totalItemCount < count )
			throw new Exception( "The total item count must be greater than or equal to the number of items." );
		foreach( var i in Enumerable.Repeat(
			        new Func<EwfTableItem<ItemIdType>>( () => throw new Exception( "Item-count placeholder items cannot be displayed." ) ),
			        itemsAndCount.totalItemCount - count ) )
			AddItem( i );

		return this;
	}

	/// <summary>
	/// Adds an item to the table. Does not defer creation of the item. Do not use this in tables that use item limiting.
	/// 
	/// You can pass EwfTableItem wherever EwfTableItem&lt;int&gt; is expected.
	/// </summary>
	public ResponsiveTable<ItemIdType> AddItem( EwfTableItem<ItemIdType> item ) {
		AddItem( () => item );
		return this;
	}

	/// <summary>
	/// Adds an item to the table. Defers creation of the item. Do not directly or indirectly create validations inside the function if they will be added to an
	/// outside data modification; this will likely cause your validations to execute in the wrong order or be skipped.
	/// 
	/// You can pass EwfTableItem wherever EwfTableItem&lt;int&gt; is expected.
	/// </summary>
	public ResponsiveTable<ItemIdType> AddItem( Func<EwfTableItem<ItemIdType>> item ) {
		if( selectedItemData.Buttons != null )
			throw new ApplicationException( "You cannot modify the table after checkboxes have been added." );

		if( hasExplicitItemGroups == true )
			throw new ApplicationException( "Item groups were previously added to the table. You cannot add both items and item groups." );
		hasExplicitItemGroups = false;

		var group = itemGroups.SingleOrDefault();
		if( group == null ) {
			var tableTailUpdateRegions = tailUpdateRegions;
			itemGroups.Add(
				group = EwfTableItemGroup.CreateWithItemIdType(
					() => new EwfTableItemGroupRemainingData( null, tailUpdateRegions: tableTailUpdateRegions ),
					Enumerable.Empty<Func<EwfTableItem<ItemIdType>>>() ) );
			tailUpdateRegions = Enumerable.Empty<TailUpdateRegion>().Materialize();
		}

		group.Items.Add( EwfTableItemGroup.GetItemLazy( item ) );
		return this;
	}

	/// <summary>
	/// Gets whether the table has items, not including head items.
	/// </summary>
	public bool HasItems => itemGroups.Any( i => i.Items.Any() );

	/// <summary>
	/// Adds a new column to the table containing a checkbox for each item with an ID. Validation will put the selected-item IDs in the specified <see cref="DataValue{T}"/>.
	/// </summary>
	/// <param name="selectedItemIds">Do not pass null.</param>
	public ResponsiveTable<ItemIdType> AddCheckboxes( DataValue<IReadOnlyCollection<ItemIdType>> selectedItemIds ) {
		TableStatics.AddCheckboxes(
			idBase,
			selectedItemActions,
			selectedItemData,
			itemGroups.Select( group => ( group.SelectedItemActions, group.Items.Select( i => new Func<EwfTableItem<ItemIdType>>( () => i.Value ) ) ) ),
			selectedItemIds,
			FormState.Current.DataModifications );
		return this;
	}

	private FlowComponent getItemLimitingControlContainer(
		string postBackIdBase, DataValue<int> currentItemLimit, UpdateRegionSet itemLimitingUpdateRegionSet,
		IReadOnlyCollection<TailUpdateRegion> tailUpdateRegions ) {
		var itemCount = itemGroups.Sum( i => i.Items.Count );
		var list = new LineList(
			new PhrasingIdContainer(
					"Item".ToQuantity( itemCount ).ToComponents(),
					updateRegionSets:
					itemGroups.SelectMany( i => i.RemainingData.Value.TailUpdateRegions ).Materialize().Concat( tailUpdateRegions ).SelectMany( i => i.Sets ) )
				.ToComponentListItem()
				.AppendLineListItem( "".ToComponentListItem() )
				.Append( "Show:".ToComponentListItem() )
				.Append( getItemLimitButtonItem( postBackIdBase, currentItemLimit, DataRowLimit.Fifty, itemLimitingUpdateRegionSet ) )
				.Append( getItemLimitButtonItem( postBackIdBase, currentItemLimit, DataRowLimit.FiveHundred, itemLimitingUpdateRegionSet ) )
				.Append( getItemLimitButtonItem( postBackIdBase, currentItemLimit, DataRowLimit.Unlimited, itemLimitingUpdateRegionSet ) ) );
		return new GenericFlowContainer( list.ToCollection(), classes: TableCssElementCreator.ItemLimitingControlContainerClass );
	}

	private ComponentListItem getItemLimitButtonItem(
		string postBackIdBase, DataValue<int> currentItemLimit, DataRowLimit itemLimit, UpdateRegionSet updateRegionSet ) {
		var text = itemLimit == DataRowLimit.Unlimited ? "All" : ( (int)itemLimit ).ToString();
		if( itemLimit == (DataRowLimit)currentItemLimit.Value )
			return text.ToComponentListItem();
		return new EwfButton(
			new StandardButtonStyle( text, buttonSize: ButtonSize.ShrinkWrap ),
			behavior: new PostBackBehavior(
				postBack: PostBack.CreateIntermediate(
					updateRegionSet.ToCollection(),
					id: PostBack.GetCompositeId( postBackIdBase, itemLimit.ToString() ),
					modificationMethod: () => currentItemLimit.Value = (int)itemLimit ) ) ).ToComponentListItem();
	}

	private IEnumerable<FlowComponent> buildRows<IdType>(
		IReadOnlyCollection<EwfTableItem<IdType>> items, IReadOnlyCollection<EwfTableField> fields, IEnumerable<PhrasingComponent> checkboxes,
		IEnumerable<IReadOnlyCollection<PhrasingComponent>> reorderingControls, bool? useContrastForFirstRow, bool useHeadCells,
		List<IReadOnlyCollection<EwfTableCell>> allVisibleItems ) {
		// Assert that the cells in the list of items are valid and store a data structure for below.
		var cellPlaceholderListsForRows = TableStatics.BuildCellPlaceholderListsForItems(
			items.Select( i => i.Cells ).Materialize(),
			fields.Count - ( checkboxes != null ? 1 : 0 ) - ( reorderingControls != null ? 1 : 0 ) );

		if( checkboxes != null )
			foreach( var i in checkboxes.Select( ( checkbox, index ) => ( checkbox, index ) ) )
				cellPlaceholderListsForRows[ i.index ].Insert( 0, i.checkbox.ToCell( setup: new TableCellSetup( containsActivatableElements: i.checkbox != null ) ) );

		if( reorderingControls != null )
			foreach( var i in reorderingControls.Select( ( control, index ) => ( control, index ) ) )
				cellPlaceholderListsForRows[ i.index ].Add( i.control.ToCell( setup: new TableCellSetup( containsActivatableElements: i.control != null ) ) );

		var rows = TableStatics.BuildRows(
			cellPlaceholderListsForRows,
			items.Select( i => i.Setup.FieldOrItemSetup ).ToImmutableArray(),
			useContrastForFirstRow,
			fields.Select( i => i.FieldOrItemSetup ).ToImmutableArray(),
			useHeadCells ? fields.Count : 0,
			false );

		allVisibleItems?.AddRange( items.Select( i => i.Cells ) );
		return rows;
	}

	private string getDataTablesColumnSpecifications( IReadOnlyCollection<EwfTableField> fields ) {
		var fieldOrItemSetups = fields.Select( i => i.FieldOrItemSetup );
		var factor = TableStatics.GetColumnWidthFactor( fieldOrItemSetups );
		return StringTools.ConcatenateWithDelimiter( ", ", fieldOrItemSetups.Select( f => getDataTablesColumnSpecification( f, factor ) ) );
	}

	private string getDataTablesColumnSpecification( EwfTableFieldOrItemSetup fieldOrItemSetup, decimal columnWidthFactor ) {
		var properties = new List<string>();
		properties.Add( "type: 'html'" );

		var width = fieldOrItemSetup.Size;
		if( width is not null ) {
			var widthValue = ( width is AncestorRelativeLength && width.Value.EndsWith( "%" )
				                   ? ( decimal.Parse( width.Value.Remove( width.Value.Length - 1 ) ) * columnWidthFactor ).ToPercentage()
				                   : width ).Value;
			properties.Add( $"width: '{widthValue}'" );
		}

		return $"{{ {StringTools.ConcatenateWithDelimiter( ", ", properties )} }}";
	}

	IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() => outerChildren;
}