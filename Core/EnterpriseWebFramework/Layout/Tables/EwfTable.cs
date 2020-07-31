﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using EnterpriseWebLibrary.IO;
using Humanizer;
using StackExchange.Profiling;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A table.
	/// </summary>
	public sealed class EwfTable: FlowComponent {
		internal static ElementClassSet GetClasses( EwfTableStyle style, ElementClassSet classes ) => getTableStyleClass( style ).Add( classes );

		private static ElementClassSet getTableStyleClass( EwfTableStyle style ) {
			switch( style ) {
				case EwfTableStyle.StandardLayoutOnly:
					return TableCssElementCreator.StandardLayoutOnlyStyleClass;
				case EwfTableStyle.StandardExceptLayout:
					return TableCssElementCreator.StandardExceptLayoutStyleClass;
				case EwfTableStyle.Standard:
					return TableCssElementCreator.StandardStyleClass;
				default:
					return ElementClassSet.Empty;
			}
		}

		internal static IReadOnlyCollection<FlowComponent> GetCaption( string caption, string subCaption ) {
			if( caption.Length == 0 )
				return Enumerable.Empty<FlowComponent>().Materialize();
			var subCaptionComponents = new List<PhrasingComponent>();
			if( subCaption.Length > 0 )
				subCaptionComponents.AddRange( new LineBreak().ToCollection().Concat( subCaption.ToComponents() ) );
			return new DisplayableElement(
				context => new DisplayableElementData(
					null,
					() => new DisplayableElementLocalData( "caption" ),
					children: caption.ToComponents().Concat( subCaptionComponents ).Materialize() ) ).ToCollection();
		}

		internal static IReadOnlyCollection<EwfTableField> GetFields(
			IReadOnlyCollection<EwfTableField> fields, IReadOnlyCollection<EwfTableItem> headItems, IEnumerable<EwfTableItem> items ) {
			var firstSpecifiedItem = headItems.Concat( items ).FirstOrDefault();
			if( firstSpecifiedItem == null )
				return Enumerable.Empty<EwfTableField>().Materialize();

			if( fields != null )
				return fields;

			// Set the fields up implicitly, based on the first item, if they weren't specified explicitly.
			var fieldCount = firstSpecifiedItem.Cells.Sum( i => i.Setup.FieldSpan );
			return Enumerable.Repeat( new EwfTableField(), fieldCount ).Materialize();
		}

		internal static decimal GetColumnWidthFactor( IEnumerable<EwfTableFieldOrItemSetup> fieldOrItemSetups ) {
			if( fieldOrItemSetups.Any( f => f.Size == null ) )
				return 1;
			return 100 / fieldOrItemSetups.Where( f => f.Size is AncestorRelativeLength && f.Size.Value.EndsWith( "%" ) )
				       .Sum( f => decimal.Parse( f.Size.Value.Remove( f.Size.Value.Length - 1 ) ) );
		}

		internal static FlowComponent GetColElement( EwfTableFieldOrItemSetup fieldOrItemSetup, decimal columnWidthFactor ) {
			var width = fieldOrItemSetup.Size;
			return new ElementComponent(
				context => new ElementData(
					() => new ElementLocalData(
						"col",
						focusDependentData: new ElementFocusDependentData(
							attributes: width != null
								            ? Tuple.Create(
										            "style",
										            "width: {0}".FormatWith(
											            ( width is AncestorRelativeLength && width.Value.EndsWith( "%" )
												              ? ( decimal.Parse( width.Value.Remove( width.Value.Length - 1 ) ) * columnWidthFactor ).ToPercentage()
												              : width ).Value ) )
									            .ToCollection()
								            : null ) ) ) );
		}

		internal static PostBack GetExportToExcelPostBack( string postBackIdBase, string caption, IReadOnlyCollection<Action<ExcelWorksheet>> rowAdders ) =>
			PostBack.CreateIntermediate(
				null,
				id: PostBack.GetCompositeId( postBackIdBase, "excel" ),
				reloadBehaviorGetter: () => new PageReloadBehavior(
					secondaryResponse: new SecondaryResponse(
						() => EwfResponse.CreateExcelWorkbookResponse(
							() => caption.Any() ? caption : "Excel export",
							() => {
								var workbook = new ExcelFileWriter();
								foreach( var i in rowAdders )
									i( workbook.DefaultWorksheet );
								return workbook;
							} ) ) ) );

		internal static IEnumerable<FlowComponent> GetGeneralActionList( PostBack exportToExcelPostBack, IReadOnlyCollection<ActionComponentSetup> actions ) {
			if( exportToExcelPostBack != null )
				actions = actions.Append( new ButtonSetup( "Export to Excel", behavior: new PostBackBehavior( postBack: exportToExcelPostBack ) ) ).Materialize();

			if( !actions.Any() )
				return Enumerable.Empty<FlowComponent>();

			return new GenericFlowContainer(
				new WrappingList(
					from action in actions
					let actionComponent = action.GetActionComponent(
						( text, icon ) => new ButtonHyperlinkStyle( text, buttonSize: ButtonSize.ShrinkWrap, icon: icon ),
						( text, icon ) => new StandardButtonStyle( text, buttonSize: ButtonSize.ShrinkWrap, icon: icon ) )
					where actionComponent != null
					select (WrappingListItem)actionComponent.ToComponentListItem( displaySetup: action.DisplaySetup ) ).ToCollection(),
				classes: TableCssElementCreator.ActionListContainerClass ).ToCollection();
		}

		/// <summary>
		/// Creates a table with one empty item group.
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
		/// <param name="defaultItemLimit">The maximum number of result items that will be shown. Default is DataRowLimit.Unlimited. A default item limit of
		/// anything other than Unlimited will cause the table to show a control allowing the user to select how many results they want to see, as well as an
		/// indicator of the total number of results that would be shown if there was no limit.</param>
		/// <param name="disableEmptyFieldDetection">Set to true if you want to disable the "at least one cell per field" assertion. Use with caution.</param>
		/// <param name="tailUpdateRegions">The tail update regions.</param>
		/// <param name="etherealContent"></param>
		public static EwfTable Create(
			DisplaySetup displaySetup = null, EwfTableStyle style = EwfTableStyle.Standard, ElementClassSet classes = null, string postBackIdBase = "",
			string caption = "", string subCaption = "", bool allowExportToExcel = false, IReadOnlyCollection<ActionComponentSetup> tableActions = null,
			IReadOnlyCollection<EwfTableField> fields = null, IReadOnlyCollection<EwfTableItem> headItems = null,
			DataRowLimit defaultItemLimit = DataRowLimit.Unlimited, bool disableEmptyFieldDetection = false,
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
				fields,
				headItems,
				defaultItemLimit,
				disableEmptyFieldDetection,
				ImmutableArray.Create(
					new EwfTableItemGroup(
						() => new EwfTableItemGroupRemainingData( null, tailUpdateRegions: tailUpdateRegions ),
						Enumerable.Empty<Func<EwfTableItem>>() ) ),
				null,
				etherealContent );

		/// <summary>
		/// Creates a table with one item group that contains the specified items.
		/// </summary>
		/// <param name="items">The items. Do not pass null.</param>
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
		/// <param name="defaultItemLimit">The maximum number of result items that will be shown. Default is DataRowLimit.Unlimited. A default item limit of
		/// anything other than Unlimited will cause the table to show a control allowing the user to select how many results they want to see, as well as an
		/// indicator of the total number of results that would be shown if there was no limit.</param>
		/// <param name="disableEmptyFieldDetection">Set to true if you want to disable the "at least one cell per field" assertion. Use with caution.</param>
		/// <param name="tailUpdateRegions">The tail update regions.</param>
		/// <param name="etherealContent"></param>
		public static EwfTable CreateWithItems(
			IEnumerable<Func<EwfTableItem>> items, DisplaySetup displaySetup = null, EwfTableStyle style = EwfTableStyle.Standard, ElementClassSet classes = null,
			string postBackIdBase = "", string caption = "", string subCaption = "", bool allowExportToExcel = false,
			IReadOnlyCollection<ActionComponentSetup> tableActions = null, IReadOnlyCollection<EwfTableField> fields = null,
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
				fields,
				headItems,
				defaultItemLimit,
				disableEmptyFieldDetection,
				ImmutableArray.Create( new EwfTableItemGroup( () => new EwfTableItemGroupRemainingData( null, tailUpdateRegions: tailUpdateRegions ), items ) ),
				null,
				etherealContent );

		/// <summary>
		/// Creates a table with multiple item groups.
		/// </summary>
		/// <param name="itemGroups">The item groups. Do not pass null.</param>
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
		/// <param name="defaultItemLimit">The maximum number of result items that will be shown. Default is DataRowLimit.Unlimited. A default item limit of
		/// anything other than Unlimited will cause the table to show a control allowing the user to select how many results they want to see, as well as an
		/// indicator of the total number of results that would be shown if there was no limit.</param>
		/// <param name="disableEmptyFieldDetection">Set to true if you want to disable the "at least one cell per field" assertion. Use with caution.</param>
		/// <param name="tailUpdateRegions">The tail update regions for the table. These operate on the item-group level, not the item level.</param>
		/// <param name="etherealContent"></param>
		public static EwfTable CreateWithItemGroups(
			IEnumerable<EwfTableItemGroup> itemGroups, DisplaySetup displaySetup = null, EwfTableStyle style = EwfTableStyle.Standard, ElementClassSet classes = null,
			string postBackIdBase = "", string caption = "", string subCaption = "", bool allowExportToExcel = false,
			IReadOnlyCollection<ActionComponentSetup> tableActions = null, IReadOnlyCollection<EwfTableField> fields = null,
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
				fields,
				headItems,
				defaultItemLimit,
				disableEmptyFieldDetection,
				itemGroups.ToImmutableArray(),
				tailUpdateRegions,
				etherealContent );

		private readonly IReadOnlyCollection<DisplayableElement> outerChildren;
		private readonly PostBack exportToExcelPostBack;
		private readonly IReadOnlyList<EwfTableItemGroup> itemGroups;

		private EwfTable(
			DisplaySetup displaySetup, EwfTableStyle style, ElementClassSet classes, string postBackIdBase, string caption, string subCaption,
			bool allowExportToExcel, IReadOnlyCollection<ActionComponentSetup> tableActions, IReadOnlyCollection<EwfTableField> specifiedFields,
			IReadOnlyCollection<EwfTableItem> headItems, DataRowLimit defaultItemLimit, bool disableEmptyFieldDetection, IReadOnlyList<EwfTableItemGroup> itemGroups,
			IReadOnlyCollection<TailUpdateRegion> tailUpdateRegions, IReadOnlyCollection<EtherealComponent> etherealContent ) {
			postBackIdBase = PostBack.GetCompositeId( "ewfTable", postBackIdBase );
			tableActions = tableActions ?? Enumerable.Empty<ActionComponentSetup>().Materialize();

			if( specifiedFields != null && !specifiedFields.Any() )
				throw new ApplicationException( "If fields are specified, there must be at least one of them." );

			headItems = headItems ?? Enumerable.Empty<EwfTableItem>().Materialize();
			tailUpdateRegions = tailUpdateRegions ?? Enumerable.Empty<TailUpdateRegion>().Materialize();

			var dataModifications = FormState.Current.DataModifications;

			var excelRowAdders = new List<Action<ExcelWorksheet>>();
			outerChildren = new DisplayableElement(
				tableContext => {
					var children = new List<FlowComponentOrNode>();
					ComponentStateItem<int> itemLimit = null;
					using( MiniProfiler.Current.Step( "EWF - Load table data" ) ) {
						FormState.ExecuteWithDataModificationsAndDefaultAction(
							dataModifications,
							() => {
								children.AddRange( GetCaption( caption, subCaption ) );

								// the maximum number of items that will be shown in this table
								itemLimit = ComponentStateItem.Create( "itemLimit", (int)defaultItemLimit, value => Enum.IsDefined( typeof( DataRowLimit ), value ) );

								var visibleItemGroupsAndItems = new List<Tuple<EwfTableItemGroup, IReadOnlyCollection<EwfTableItem>>>();
								foreach( var itemGroup in itemGroups ) {
									var visibleItems = itemGroup.Items.Take( itemLimit.Value.Value - visibleItemGroupsAndItems.Sum( i => i.Item2.Count ) ).Select( i => i() );
									visibleItemGroupsAndItems.Add( Tuple.Create( itemGroup, visibleItems.Materialize() ) );
									if( visibleItemGroupsAndItems.Sum( i => i.Item2.Count ) == itemLimit.Value.Value )
										break;
								}

								var fields = GetFields( specifiedFields, headItems, visibleItemGroupsAndItems.SelectMany( i => i.Item2 ) );
								if( !fields.Any() )
									fields = new EwfTableField().ToCollection();

								children.AddRange( getColumnSpecifications( fields ) );

								var allVisibleItems = new List<EwfTableItem>();

								var itemLimitingUpdateRegionSet = new UpdateRegionSet();
								var itemLimitingAndGeneralActionComponents =
									( defaultItemLimit != DataRowLimit.Unlimited
										  ? getItemLimitingControlContainer( postBackIdBase, itemLimit.Value, itemLimitingUpdateRegionSet, tailUpdateRegions ).ToCollection()
										  : Enumerable.Empty<FlowComponent>() ).Concat( GetGeneralActionList( allowExportToExcel ? exportToExcelPostBack : null, tableActions ) )
									.Materialize();
								var headRows = buildRows(
										( itemLimitingAndGeneralActionComponents.Any()
											  ? new EwfTableItem(
													  new GenericFlowContainer(
														  itemLimitingAndGeneralActionComponents,
														  classes: TableCssElementCreator.ItemLimitingAndGeneralActionContainerClass ).ToCell(
														  new TableCellSetup( fieldSpan: fields.Count ) ) )
												  .ToCollection()
											  : Enumerable.Empty<EwfTableItem>() ).Concat( getItemActionsItem( fields.Count ) )
										.Materialize(),
										Enumerable.Repeat( new EwfTableField(), fields.Count ).Materialize(),
										null,
										false,
										null,
										null,
										allVisibleItems )
									.Concat( buildRows( headItems, fields, null, true, null, null, allVisibleItems ) )
									.Materialize();
								if( headRows.Any() )
									children.Add( new ElementComponent( context => new ElementData( () => new ElementLocalData( "thead" ), children: headRows ) ) );
								excelRowAdders.AddRange( headItems.Select( i => TableStatics.GetExcelRowAdder( true, i.Cells ) ) );

								var bodyRowGroupsAndRows = new List<Tuple<FlowComponent, IReadOnlyCollection<FlowComponent>>>();
								var updateRegionSetListsAndStaticRowGroupCounts = new List<Tuple<IReadOnlyCollection<UpdateRegionSet>, int>>();
								for( var visibleGroupIndex = 0; visibleGroupIndex < visibleItemGroupsAndItems.Count; visibleGroupIndex += 1 ) {
									var groupAndItems = visibleItemGroupsAndItems[ visibleGroupIndex ];
									var useContrastForFirstRow = visibleItemGroupsAndItems.Where( ( group, i ) => i < visibleGroupIndex ).Sum( i => i.Item2.Count ) % 2 == 1;
									var groupBodyRows = buildRows( groupAndItems.Item2, fields, useContrastForFirstRow, false, null, null, allVisibleItems ).Materialize();
									var cachedVisibleGroupIndex = visibleGroupIndex;
									FlowComponent rowGroup = new FlowIdContainer(
										new ElementComponent(
											context => new ElementData(
												() => new ElementLocalData( "tbody" ),
												children: buildRows(
														groupAndItems.Item1.GetHeadItems( fields.Count ),
														Enumerable.Repeat( new EwfTableField(), fields.Count ).ToArray(),
														null,
														true,
														null,
														null,
														allVisibleItems )
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
										from region in tailUpdateRegions.Select( i => new { sets = i.Sets, staticRowGroupCount = itemGroups.Count - i.UpdatingItemCount } )
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
									var item = new EwfTableItem( button.ToCollection().ToCell( new TableCellSetup( fieldSpan: fields.Count ) ) );
									var useContrast = visibleItemGroupsAndItems.Sum( i => i.Item2.Count ) % 2 == 1;
									itemLimitingRowGroup.Add(
										new ElementComponent(
											context => new ElementData(
												() => new ElementLocalData( "tbody" ),
												children: buildRows(
														item.ToCollection().ToList(),
														Enumerable.Repeat( new EwfTableField(), fields.Count ).ToArray(),
														useContrast,
														false,
														null,
														null,
														allVisibleItems )
													.Materialize() ) ) );
								}
								children.Add(
									new FlowIdContainer(
										itemLimitingRowGroup,
										updateRegionSets: itemLimitingUpdateRegionSet.ToCollection()
											.Concat(
												itemGroups.SelectMany( i => i.RemainingData.Value.TailUpdateRegions )
													.Materialize()
													.Concat( tailUpdateRegions )
													.SelectMany( i => i.Sets ) ) ) );

								// Assert that every visible item in the table has the same number of cells and store a data structure for below.
								var cellPlaceholderListsForItems = TableStatics.BuildCellPlaceholderListsForItems( allVisibleItems, fields.Count );

								if( !disableEmptyFieldDetection )
									TableStatics.AssertAtLeastOneCellPerField( fields, cellPlaceholderListsForItems );
							} );
					}
					return new DisplayableElementData(
						displaySetup,
						() => new DisplayableElementLocalData( "table" ),
						classes: GetClasses( style, classes ?? ElementClassSet.Empty ),
						children: children,
						etherealChildren: ( defaultItemLimit != DataRowLimit.Unlimited ? itemLimit.ToCollection() : Enumerable.Empty<EtherealComponent>() )
						.Concat( etherealContent ?? Enumerable.Empty<EtherealComponent>() )
						.Materialize() );
				} ).ToCollection();

			exportToExcelPostBack = GetExportToExcelPostBack( postBackIdBase, caption, excelRowAdders );

			this.itemGroups = itemGroups;
		}

		/// <summary>
		/// Gets the Export to Excel post-back. This is convenient if you want to use the built-in export functionality, but from an external button.
		/// </summary>
		public PostBack ExportToExcelPostBack => exportToExcelPostBack;

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

		/// <summary>
		/// Gets whether the table has items, not including head items.
		/// </summary>
		public bool HasItems => itemGroups.Any( i => i.Items.Any() );

		private IReadOnlyCollection<FlowComponent> getColumnSpecifications( IReadOnlyCollection<EwfTableField> fields ) {
			var fieldOrItemSetups = fields.Select( i => i.FieldOrItemSetup );
			var factor = GetColumnWidthFactor( fieldOrItemSetups );
			return fieldOrItemSetups.Select( f => GetColElement( f, factor ) ).Materialize();
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

		private EwfTableItem[] getItemActionsItem( int fieldCount ) {
			// NOTE: Build a head group row for check box selection (all/none) and check box actions, if evaluated items have check box actions.
			// NOTE: Go through all visible items and build a list of their tablewide check box actions. Make sure all items have identical lists.
			// NOTE: Make sure every item in the list has the same action names. If this holds, draw the row.
			// NOTE: Check box actions should show an error if clicked and no items are selected; this caused confusion in M+Vision.
			return new EwfTableItem[ 0 ];
		}

		private IEnumerable<FlowComponent> buildRows(
			IReadOnlyCollection<EwfTableItem> items, IReadOnlyCollection<EwfTableField> fields, bool? useContrastForFirstRow, bool useHeadCells,
			Func<EwfTableCell> itemActionCheckBoxCellGetter, Func<EwfTableCell> itemReorderingCellGetter, List<EwfTableItem> allVisibleItems ) {
			// Assert that the cells in the list of items are valid and store a data structure for below.
			var cellPlaceholderListsForRows = TableStatics.BuildCellPlaceholderListsForItems( items, fields.Count );

			// NOTE: Be sure to take check box and reordering columns into account.
			var rows = TableStatics.BuildRows(
				cellPlaceholderListsForRows,
				items.Select( i => i.Setup.FieldOrItemSetup ).ToImmutableArray(),
				useContrastForFirstRow,
				fields.Select( i => i.FieldOrItemSetup ).ToImmutableArray(),
				useHeadCells ? fields.Count : 0,
				false );

			allVisibleItems.AddRange( items );
			return rows;
		}

		IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() => outerChildren;
	}
}