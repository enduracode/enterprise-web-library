using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using EnterpriseWebLibrary.IO;
using Humanizer;
using StackExchange.Profiling;
using static MoreLinq.Extensions.EquiZipExtension;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A table.
	/// </summary>
	public class EwfTable: EwfTable<int> {
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
		/// <param name="selectedItemActions">Table selected-item actions. Passing one or more of these will add a new column to the table containing a checkbox for
		/// each item with an ID. If you would like the table to support item-group-level selected-item actions, you must pass a collection here even if it is
		/// empty.</param>
		/// <param name="fields">The table's fields. Do not pass an empty collection.</param>
		/// <param name="headItems">The table's head items.</param>
		/// <param name="defaultItemLimit">The maximum number of result items that will be shown. Default is DataRowLimit.Unlimited. A default item limit of
		/// anything other than Unlimited will cause the table to show a control allowing the user to select how many results they want to see, as well as an
		/// indicator of the total number of results that would be shown if there was no limit.</param>
		/// <param name="disableEmptyFieldDetection">Set to true if you want to disable the "at least one cell per field" assertion. Use with caution.</param>
		/// <param name="tailUpdateRegions">The tail update regions for the table, which will operate on the item level if you add items, or the item-group level if
		/// you add item groups.</param>
		/// <param name="etherealContent"></param>
		public static EwfTable Create(
			DisplaySetup displaySetup = null, EwfTableStyle style = EwfTableStyle.Standard, ElementClassSet classes = null, string postBackIdBase = "",
			string caption = "", string subCaption = "", bool allowExportToExcel = false, IReadOnlyCollection<ActionComponentSetup> tableActions = null,
			IReadOnlyCollection<SelectedItemAction<int>> selectedItemActions = null, IReadOnlyCollection<EwfTableField> fields = null,
			IReadOnlyCollection<EwfTableItem> headItems = null, DataRowLimit defaultItemLimit = DataRowLimit.Unlimited, bool disableEmptyFieldDetection = false,
			IReadOnlyCollection<TailUpdateRegion> tailUpdateRegions = null, IReadOnlyCollection<EtherealComponent> etherealContent = null ) =>
			new EwfTable(
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
				disableEmptyFieldDetection,
				tailUpdateRegions,
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
		/// <param name="selectedItemActions">Table selected-item actions. Passing one or more of these will add a new column to the table containing a checkbox for
		/// each item with an ID. If you would like the table to support item-group-level selected-item actions, you must pass a collection here even if it is
		/// empty.</param>
		/// <param name="fields">The table's fields. Do not pass an empty collection.</param>
		/// <param name="headItems">The table's head items.</param>
		/// <param name="defaultItemLimit">The maximum number of result items that will be shown. Default is DataRowLimit.Unlimited. A default item limit of
		/// anything other than Unlimited will cause the table to show a control allowing the user to select how many results they want to see, as well as an
		/// indicator of the total number of results that would be shown if there was no limit.</param>
		/// <param name="disableEmptyFieldDetection">Set to true if you want to disable the "at least one cell per field" assertion. Use with caution.</param>
		/// <param name="tailUpdateRegions">The tail update regions for the table, which will operate on the item level if you add items, or the item-group level if
		/// you add item groups.</param>
		/// <param name="etherealContent"></param>
		public static EwfTable<ItemIdType> CreateWithItemIdType<ItemIdType>(
			DisplaySetup displaySetup = null, EwfTableStyle style = EwfTableStyle.Standard, ElementClassSet classes = null, string postBackIdBase = "",
			string caption = "", string subCaption = "", bool allowExportToExcel = false, IReadOnlyCollection<ActionComponentSetup> tableActions = null,
			IReadOnlyCollection<SelectedItemAction<ItemIdType>> selectedItemActions = null, IReadOnlyCollection<EwfTableField> fields = null,
			IReadOnlyCollection<EwfTableItem> headItems = null, DataRowLimit defaultItemLimit = DataRowLimit.Unlimited, bool disableEmptyFieldDetection = false,
			IReadOnlyCollection<TailUpdateRegion> tailUpdateRegions = null, IReadOnlyCollection<EtherealComponent> etherealContent = null ) =>
			new EwfTable<ItemIdType>(
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
				disableEmptyFieldDetection,
				tailUpdateRegions,
				etherealContent );

		private EwfTable(
			DisplaySetup displaySetup, EwfTableStyle style, ElementClassSet classes, string postBackIdBase, string caption, string subCaption,
			bool allowExportToExcel, IReadOnlyCollection<ActionComponentSetup> tableActions, IReadOnlyCollection<SelectedItemAction<int>> selectedItemActions,
			IReadOnlyCollection<EwfTableField> fields, IReadOnlyCollection<EwfTableItem> headItems, DataRowLimit defaultItemLimit, bool disableEmptyFieldDetection,
			IReadOnlyCollection<TailUpdateRegion> tailUpdateRegions, IReadOnlyCollection<EtherealComponent> etherealContent ): base(
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
			disableEmptyFieldDetection,
			tailUpdateRegions,
			etherealContent ) {}
	}

	/// <summary>
	/// A table.
	/// </summary>
	public class EwfTable<ItemIdType>: FlowComponent {
		private readonly IReadOnlyCollection<DisplayableElement> outerChildren;
		private readonly string postBackIdBase;
		private readonly PostBack exportToExcelPostBack;
		private readonly IReadOnlyCollection<SelectedItemAction<ItemIdType>> selectedItemActions;
		private readonly TableSelectedItemData<ItemIdType> selectedItemData = new TableSelectedItemData<ItemIdType>();
		private readonly List<EwfTableItemGroup<ItemIdType>> itemGroups = new List<EwfTableItemGroup<ItemIdType>>();
		private bool? hasExplicitItemGroups;
		private IReadOnlyCollection<TailUpdateRegion> tailUpdateRegions;

		internal EwfTable(
			DisplaySetup displaySetup, EwfTableStyle style, ElementClassSet classes, string postBackIdBase, string caption, string subCaption,
			bool allowExportToExcel, IReadOnlyCollection<ActionComponentSetup> tableActions, IReadOnlyCollection<SelectedItemAction<ItemIdType>> selectedItemActions,
			IReadOnlyCollection<EwfTableField> fields, IReadOnlyCollection<EwfTableItem> headItems, DataRowLimit defaultItemLimit, bool disableEmptyFieldDetection,
			IReadOnlyCollection<TailUpdateRegion> tailUpdateRegions, IReadOnlyCollection<EtherealComponent> etherealContent ) {
			postBackIdBase = PostBack.GetCompositeId( "ewfTable", postBackIdBase );
			tableActions = tableActions ?? Enumerable.Empty<ActionComponentSetup>().Materialize();

			if( fields != null && !fields.Any() )
				throw new ApplicationException( "If fields are specified, there must be at least one of them." );

			headItems = headItems ?? Enumerable.Empty<EwfTableItem>().Materialize();
			tailUpdateRegions = tailUpdateRegions ?? Enumerable.Empty<TailUpdateRegion>().Materialize();

			var dataModifications = FormState.Current.DataModifications;

			var excelRowAdders = new List<Action<ExcelWorksheet>>();
			outerChildren = new DisplayableElement(
				tableContext => {
					if( selectedItemData.Buttons == null )
						TableStatics.AddCheckboxes(
							postBackIdBase,
							selectedItemActions,
							selectedItemData,
							itemGroups.Select( group => ( group.SelectedItemActions, group.Items.Select( i => new Func<EwfTableItem<ItemIdType>>( () => i.Value ) ) ) ),
							null,
							Enumerable.Empty<DataModification>().Materialize() );

					var children = new List<FlowComponentOrNode>();
					ComponentStateItem<int> itemLimit = null;
					using( MiniProfiler.Current.Step( "EWF - Load table data" ) ) {
						FormState.ExecuteWithDataModificationsAndDefaultAction(
							dataModifications,
							() => {
								children.AddRange( TableStatics.GetCaption( caption, subCaption ) );

								// the maximum number of items that will be shown in this table
								itemLimit = ComponentStateItem.Create( "itemLimit", (int)defaultItemLimit, value => Enum.IsDefined( typeof( DataRowLimit ), value ) );

								var visibleItemGroupsAndItems = new List<Tuple<EwfTableItemGroup<ItemIdType>, IReadOnlyCollection<EwfTableItem<ItemIdType>>>>();
								foreach( var itemGroup in itemGroups ) {
									var visibleItems = itemGroup.Items.Take( itemLimit.Value.Value - visibleItemGroupsAndItems.Sum( i => i.Item2.Count ) ).Select( i => i.Value );
									visibleItemGroupsAndItems.Add( Tuple.Create( itemGroup, visibleItems.Materialize() ) );
									if( visibleItemGroupsAndItems.Sum( i => i.Item2.Count ) == itemLimit.Value.Value )
										break;
								}

								TableStatics.AssertItemIdsUnique( visibleItemGroupsAndItems.SelectMany( i => i.Item2 ) );

								fields = TableStatics.GetFields( fields, headItems, visibleItemGroupsAndItems.SelectMany( i => i.Item2 ) );
								if( !fields.Any() )
									fields = new EwfTableField().ToCollection();
								else if( selectedItemData.ItemGroupData != null )
									fields = fields.Prepend( new EwfTableField( size: 2.ToEm() ) ).Materialize();

								children.AddRange( getColumnSpecifications( fields ) );

								var allVisibleItems = new List<IReadOnlyCollection<EwfTableCell>>();

								var itemLimitingUpdateRegionSet = new UpdateRegionSet();
								var itemLimitingAndGeneralActionComponents =
									( defaultItemLimit != DataRowLimit.Unlimited
										  ? getItemLimitingControlContainer( postBackIdBase, itemLimit.Value, itemLimitingUpdateRegionSet, this.tailUpdateRegions ).ToCollection()
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
											  : Enumerable.Empty<EwfTableItem>() ).Concat(
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
										false,
										null,
										null )
									.Concat(
										buildRows(
											headItems,
											fields,
											selectedItemData.ItemGroupData == null ? null : Enumerable.Repeat( (PhrasingComponent)null, headItems.Count ),
											null,
											true,
											null,
											allVisibleItems ) )
									.Materialize();
								if( headRows.Any() )
									children.Add( new ElementComponent( context => new ElementData( () => new ElementLocalData( "thead" ), children: headRows ) ) );
								excelRowAdders.AddRange( headItems.Select( i => TableStatics.GetExcelRowAdder( true, i.Cells ) ) );

								var bodyRowGroupsAndRows = new List<Tuple<FlowComponent, IReadOnlyCollection<FlowComponent>>>();
								var updateRegionSetListsAndStaticRowGroupCounts = new List<Tuple<IReadOnlyCollection<UpdateRegionSet>, int>>();
								for( var visibleGroupIndex = 0; visibleGroupIndex < visibleItemGroupsAndItems.Count; visibleGroupIndex += 1 ) {
									var groupAndItems = visibleItemGroupsAndItems[ visibleGroupIndex ];
									var groupSelectedItemData = selectedItemData.ItemGroupData?[ visibleGroupIndex ];
									var useContrastForFirstRow = visibleItemGroupsAndItems.Where( ( group, i ) => i < visibleGroupIndex ).Sum( i => i.Item2.Count ) % 2 == 1;
									var groupBodyRows = buildRows(
											groupAndItems.Item2,
											fields,
											selectedItemData.ItemGroupData == null ? null :
											!groupSelectedItemData.HasValue ? Enumerable.Repeat( (PhrasingComponent)null, groupAndItems.Item2.Count ) :
											groupSelectedItemData.Value.checkboxes.Take( groupAndItems.Item2.Count )
												.EquiZip( groupAndItems.Item2, ( checkbox, item ) => item.Setup.Id != null ? checkbox : null ),
											useContrastForFirstRow,
											false,
											null,
											allVisibleItems )
										.Materialize();
									var cachedVisibleGroupIndex = visibleGroupIndex;
									FlowComponent rowGroup = new FlowIdContainer(
										new ElementComponent(
											context => new ElementData(
												() => new ElementLocalData( "tbody" ),
												children:
												buildRows(
														groupAndItems.Item1.GetHeadItem( fields.Count ),
														Enumerable.Repeat( new EwfTableField(), fields.Count ).Materialize(),
														null,
														null,
														true,
														null,
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
															false,
															null,
															null ) )
													.Append<FlowComponentOrNode>(
														new IdentifiedFlowComponent(
															() => new IdentifiedComponentData<FlowComponentOrNode>(
																"",
																new UpdateRegionLinker(
																	"tail",
																	from region in groupAndItems.Item1.RemainingData.Value.TailUpdateRegions
																	let staticRowCount = itemGroups[ cachedVisibleGroupIndex ].Items.Count - region.UpdatingItemCount
																	select new PreModificationUpdateRegion( region.Sets, () => groupBodyRows.Skip( staticRowCount ), staticRowCount.ToString ),
																	arg => groupBodyRows.Skip( int.Parse( arg ) ) ).ToCollection(),
																new ErrorSourceSet(),
																errorsBySource => groupBodyRows ) ) )
													.Materialize() ) ).ToCollection() );
									bodyRowGroupsAndRows.Add( Tuple.Create( rowGroup, groupBodyRows ) );
									excelRowAdders.AddRange( groupAndItems.Item2.Select( i => TableStatics.GetExcelRowAdder( false, i.Cells ) ) );

									// If item limiting is enabled, include all subsequent item groups in tail update regions since any number of items could be appended.
									if( defaultItemLimit != DataRowLimit.Unlimited )
										updateRegionSetListsAndStaticRowGroupCounts.Add(
											Tuple.Create(
												groupAndItems.Item1.RemainingData.Value.TailUpdateRegions.SelectMany( i => i.Sets ).Materialize(),
												visibleGroupIndex + 1 ) );
								}
								var linkers = new List<UpdateRegionLinker>();
								children.Add(
									new IdentifiedFlowComponent(
										() => new IdentifiedComponentData<FlowComponentOrNode>(
											"",
											linkers,
											new ErrorSourceSet(),
											errorsBySource => bodyRowGroupsAndRows.Select( i => i.Item1 ) ) ) );

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
										from region in this.tailUpdateRegions.Select( i => new { sets = i.Sets, staticRowGroupCount = itemGroups.Count - i.UpdatingItemCount } )
											.Concat( updateRegionSetListsAndStaticRowGroupCounts.Select( i => new { sets = i.Item1, staticRowGroupCount = i.Item2 } ) )
										select new PreModificationUpdateRegion(
											region.sets,
											() => bodyRowGroupsAndRows.Skip( region.staticRowGroupCount ).Select( i => i.Item1 ),
											region.staticRowGroupCount.ToString ),
										arg => bodyRowGroupsAndRows.Skip( int.Parse( arg ) ).Select( i => i.Item1 ) ) );

								var itemCount = itemGroups.Sum( i => i.Items.Count );
								var itemLimitingRowGroup = new List<FlowComponent>();
								if( itemLimit.Value.Value < itemCount ) {
									var nextLimit = EnumTools.GetValues<DataRowLimit>().First( i => i > (DataRowLimit)itemLimit.Value.Value );
									var itemIncrementCount = Math.Min( (int)nextLimit, itemCount ) - itemLimit.Value.Value;
									var button = new EwfButton(
										new StandardButtonStyle( "Show " + itemIncrementCount + " more item" + ( itemIncrementCount != 1 ? "s" : "" ) ),
										behavior: new PostBackBehavior(
											postBack: PostBack.CreateIntermediate(
												itemLimitingUpdateRegionSet.ToCollection(),
												id: PostBack.GetCompositeId( postBackIdBase, "showMore" ),
												firstModificationMethod: () => itemLimit.Value.Value = (int)nextLimit ) ) );
									var item = EwfTableItem.Create( button.ToCollection().ToCell( new TableCellSetup( fieldSpan: fields.Count ) ) );
									var useContrast = visibleItemGroupsAndItems.Sum( i => i.Item2.Count ) % 2 == 1;
									itemLimitingRowGroup.Add(
										new ElementComponent(
											context => new ElementData(
												() => new ElementLocalData( "tbody" ),
												children: buildRows(
														item.ToCollection(),
														Enumerable.Repeat( new EwfTableField(), fields.Count ).Materialize(),
														null,
														useContrast,
														false,
														null,
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
								var fieldCount = fields.Count - ( selectedItemData.ItemGroupData != null ? 1 : 0 );
								var cellPlaceholderListsForItems = TableStatics.BuildCellPlaceholderListsForItems( allVisibleItems, fieldCount );

								if( !disableEmptyFieldDetection )
									TableStatics.AssertAtLeastOneCellPerField( fieldCount, cellPlaceholderListsForItems );
							} );
					}
					return new DisplayableElementData(
						displaySetup,
						() => new DisplayableElementLocalData( "table" ),
						classes: TableStatics.GetClasses( style, classes ?? ElementClassSet.Empty ),
						children: children,
						etherealChildren: ( defaultItemLimit != DataRowLimit.Unlimited ? itemLimit.ToCollection() : Enumerable.Empty<EtherealComponent>() )
						.Concat( etherealContent ?? Enumerable.Empty<EtherealComponent>() )
						.Materialize() );
				} ).ToCollection();

			this.postBackIdBase = postBackIdBase;
			exportToExcelPostBack = TableStatics.GetExportToExcelPostBack( postBackIdBase, caption, excelRowAdders );
			this.selectedItemActions = selectedItemActions;
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
		public EwfTable<ItemIdType> AddData<T>( IEnumerable<T> data, Func<T, EwfTableItem<ItemIdType>> itemSelector ) {
			data.ToList().ForEach( d => AddItem( () => itemSelector( d ) ) );
			return this;
		}

		/// <summary>
		/// Adds an item to the table. Does not defer creation of the item. Do not use this in tables that use item limiting.
		/// 
		/// You can pass EwfTableItem wherever EwfTableItem&lt;int&gt; is expected.
		/// </summary>
		public EwfTable<ItemIdType> AddItem( EwfTableItem<ItemIdType> item ) {
			AddItem( () => item );
			return this;
		}

		/// <summary>
		/// Adds an item to the table. Defers creation of the item. Do not directly or indirectly create validations inside the function if they will be added to a
		/// validation list that exists outside the function; this will likely cause your validations to execute in the wrong order or be skipped.
		/// 
		/// You can pass EwfTableItem wherever EwfTableItem&lt;int&gt; is expected.
		/// </summary>
		public EwfTable<ItemIdType> AddItem( Func<EwfTableItem<ItemIdType>> item ) {
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
		/// Adds item groups to the table.
		/// 
		/// You can pass EwfTableItemGroup wherever EwfTableItemGroup&lt;int&gt; is expected.
		/// </summary>
		public EwfTable<ItemIdType> AddItemGroups( IReadOnlyCollection<EwfTableItemGroup<ItemIdType>> itemGroups ) {
			foreach( var i in itemGroups )
				AddItemGroup( i );
			return this;
		}

		/// <summary>
		/// Adds an item group to the table.
		/// 
		/// You can pass EwfTableItemGroup wherever EwfTableItemGroup&lt;int&gt; is expected.
		/// </summary>
		public EwfTable<ItemIdType> AddItemGroup( EwfTableItemGroup<ItemIdType> itemGroup ) {
			if( selectedItemData.Buttons != null )
				throw new ApplicationException( "You cannot modify the table after checkboxes have been added." );

			if( hasExplicitItemGroups == false )
				throw new ApplicationException( "Items were previously added to the table. You cannot add both items and item groups." );
			hasExplicitItemGroups = true;

			if( itemGroup.SelectedItemActions.Any() && selectedItemActions == null )
				throw new ApplicationException( "Selected-item actions are disabled." );

			itemGroups.Add( itemGroup );
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
		public EwfTable<ItemIdType> AddCheckboxes( DataValue<IReadOnlyCollection<ItemIdType>> selectedItemIds ) {
			TableStatics.AddCheckboxes(
				postBackIdBase,
				selectedItemActions,
				selectedItemData,
				itemGroups.Select( group => ( group.SelectedItemActions, group.Items.Select( i => new Func<EwfTableItem<ItemIdType>>( () => i.Value ) ) ) ),
				selectedItemIds,
				FormState.Current.DataModifications );
			return this;
		}

		private IReadOnlyCollection<FlowComponent> getColumnSpecifications( IReadOnlyCollection<EwfTableField> fields ) {
			var fieldOrItemSetups = fields.Select( i => i.FieldOrItemSetup );
			var factor = TableStatics.GetColumnWidthFactor( fieldOrItemSetups );
			return fieldOrItemSetups.Select( f => TableStatics.GetColElement( f, factor ) ).Materialize();
		}

		private FlowComponent getItemLimitingControlContainer(
			string postBackIdBase, DataValue<int> currentItemLimit, UpdateRegionSet itemLimitingUpdateRegionSet,
			IReadOnlyCollection<TailUpdateRegion> tailUpdateRegions ) {
			var itemCount = itemGroups.Sum( i => i.Items.Count );
			var list = new LineList(
				new PhrasingIdContainer(
						"Item".ToQuantity( itemCount ).ToComponents(),
						updateRegionSets: itemGroups.SelectMany( i => i.RemainingData.Value.TailUpdateRegions )
							.Materialize()
							.Concat( tailUpdateRegions )
							.SelectMany( i => i.Sets ) ).ToComponentListItem()
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
						firstModificationMethod: () => currentItemLimit.Value = (int)itemLimit ) ) ).ToComponentListItem();
		}

		private IEnumerable<FlowComponent> buildRows<IdType>(
			IReadOnlyCollection<EwfTableItem<IdType>> items, IReadOnlyCollection<EwfTableField> fields, IEnumerable<PhrasingComponent> checkboxes,
			bool? useContrastForFirstRow, bool useHeadCells, Func<EwfTableCell> itemReorderingCellGetter, List<IReadOnlyCollection<EwfTableCell>> allVisibleItems ) {
			// Assert that the cells in the list of items are valid and store a data structure for below.
			var cellPlaceholderListsForRows = TableStatics.BuildCellPlaceholderListsForItems(
				items.Select( i => i.Cells ).Materialize(),
				fields.Count - ( checkboxes != null ? 1 : 0 ) );

			if( checkboxes != null )
				foreach( var i in checkboxes.Select( ( checkbox, index ) => ( checkbox, index ) ) )
					cellPlaceholderListsForRows[ i.index ].Insert( 0, i.checkbox.ToCell( setup: new TableCellSetup( containsActivatableElements: i.checkbox != null ) ) );

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

		IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() => outerChildren;
	}
}