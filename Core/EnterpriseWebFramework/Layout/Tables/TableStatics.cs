using System.Collections.Immutable;
using EnterpriseWebLibrary.DataAccess.Ranking;
using Tewl.IO;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

internal static class TableStatics {
	// This class name is used by EWF CSS and JavaScript files.
	private static readonly ElementClass activatableElementContainerClass = new( "ewfAec" );

	internal static void AddCheckboxes<ItemIdType>(
		string postBackIdBase, IReadOnlyCollection<SelectedItemAction<ItemIdType>>? selectedItemActions, TableSelectedItemData<ItemIdType> selectedItemData,
		IEnumerable<( IReadOnlyCollection<SelectedItemAction<ItemIdType>> selectedItemActions, IEnumerable<Func<EwfTableItem<ItemIdType>>> itemGetters )>
			itemGroups, DataValue<IReadOnlyCollection<ItemIdType>>? selectedItemIds, IReadOnlyCollection<DataModification> externalDataModifications ) {
		var tablePostBackAndButtonPairs = ( selectedItemActions ?? Enumerable.Empty<SelectedItemAction<ItemIdType>>() ).Select(
				action => action.GetPostBackAndButton( postBackIdBase, () => selectedItemData.ItemGroupData.SelectMany( i => i!.Value.selectedIds ).Materialize() ) )
			.Materialize();
		selectedItemData.Buttons = tablePostBackAndButtonPairs.Select( i => i.button ).Materialize();

		if( selectedItemActions == null && selectedItemIds == null )
			return;

		selectedItemData.ItemGroupData = itemGroups.Select(
				group => {
					var groupSelectedItemIds = new List<ItemIdType?>();
					var groupPostBackAndButtonPairs = group.selectedItemActions.Select( i => i.GetPostBackAndButton( postBackIdBase, () => groupSelectedItemIds ) )
						.Materialize();

					var dataModifications = externalDataModifications.Concat( tablePostBackAndButtonPairs.Select( i => i.postBack ) )
						.Concat( groupPostBackAndButtonPairs.Select( i => i.postBack ) )
						.Materialize();
					if( !dataModifications.Any() )
						return (( IReadOnlyCollection<ButtonSetup>, EwfValidation?, IReadOnlyCollection<PhrasingComponent>, List<ItemIdType?> )?)null;

					var checkboxes = FormState.ExecuteWithDataModificationsAndDefaultAction(
						dataModifications,
						() => group.itemGetters.Select(
								i => new Checkbox(
									false,
									Enumerable.Empty<PhrasingComponent>().Materialize(),
									validationMethod: ( postBackValue, _ ) => {
										if( postBackValue.Value )
											groupSelectedItemIds.Add( i().Setup.Id!.Value );
									} ).PageComponent )
							.Materialize(),
						formControlDefaultActionOverride: new SpecifiedValue<NonPostBackFormAction>( null ) );

					var validation = groupPostBackAndButtonPairs.Any()
						                 ? FormState.ExecuteWithDataModificationsAndDefaultAction(
							                 groupPostBackAndButtonPairs.Select( i => i.postBack ),
							                 () => new EwfValidation(
								                 validator => {
									                 if( !groupSelectedItemIds.Any() )
										                 validator.NoteErrorAndAddMessage( "Please select at least one item." );
								                 } ) )
						                 : null;

					return ( groupPostBackAndButtonPairs.Select( i => i.button ).Materialize(), validation, checkboxes, groupSelectedItemIds );
				} )
			.ToImmutableArray();

		if( tablePostBackAndButtonPairs.Any() )
			FormState.ExecuteWithDataModificationsAndDefaultAction(
				tablePostBackAndButtonPairs.Select( i => i.postBack ),
				() => selectedItemData.Validation = new EwfValidation(
					      validator => {
						      if( !selectedItemData.ItemGroupData.SelectMany( i => i!.Value.selectedIds ).Any() )
							      validator.NoteErrorAndAddMessage( "Please select at least one item." );
					      } ) );

		if( selectedItemIds != null )
			new EwfValidation( _ => selectedItemIds.Value = selectedItemData.ItemGroupData.SelectMany( i => i!.Value.selectedIds ).Materialize() );
	}

	internal static void AssertItemIdsUnique<ItemIdType>( IEnumerable<EwfTableItem<ItemIdType>> items ) {
		if( items.Where( i => i.Setup.Id is not null ).Select( i => i.Setup.Id!.Value ).GetDuplicates().Any() )
			throw new ApplicationException( "Item IDs must be unique." );
	}

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
			_ => new DisplayableElementData(
				null,
				() => new DisplayableElementLocalData( "caption" ),
				children: caption.ToComponents().Concat( subCaptionComponents ).Materialize() ) ).ToCollection();
	}

	internal static IReadOnlyCollection<EwfTableField> GetFields<ItemIdType>(
		IReadOnlyCollection<EwfTableField>? fields, IReadOnlyCollection<EwfTableItem> headItems, IEnumerable<EwfTableItem<ItemIdType>> items ) {
		var firstSpecifiedItemCells = headItems.Select( i => i.Cells ).Concat( items.Select( i => i.Cells ) ).FirstOrDefault();
		if( firstSpecifiedItemCells == null )
			return Enumerable.Empty<EwfTableField>().Materialize();

		if( fields is not null )
			return fields;

		// Set the fields up implicitly, based on the first item, if they weren't specified explicitly.
		var fieldCount = firstSpecifiedItemCells.Sum( i => i.Setup.FieldSpan );
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
			_ => new ElementData(
				() => new ElementLocalData(
					"col",
					focusDependentData: new ElementFocusDependentData(
						attributes: width != null
							            ? new ElementAttribute(
								            "style",
								            "width: {0}".FormatWith(
									            ( width is AncestorRelativeLength && width.Value.EndsWith( "%" )
										              ? ( decimal.Parse( width.Value.Remove( width.Value.Length - 1 ) ) * columnWidthFactor ).ToPercentage()
										              : width ).Value ) ).ToCollection()
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

	internal static IEnumerable<FlowComponent> GetGeneralActionList( PostBack? exportToExcelPostBack, IReadOnlyCollection<ActionComponentSetup> actions ) {
		if( exportToExcelPostBack is not null )
			actions = actions.Append( new ButtonSetup( "Export to Excel", behavior: new PostBackBehavior( postBack: exportToExcelPostBack ) ) ).Materialize();
		return GetActionList( actions );
	}

	internal static IReadOnlyCollection<FlowComponent>
		GetItemSelectionAndActionComponents( string checkboxCellSelector, IReadOnlyCollection<ButtonSetup> buttons, EwfValidation? validation ) =>
		new DisplayableElement(
				context => new DisplayableElementData(
					null,
					() => ListErrorDisplayStyle.GetErrorFocusableElementLocalData( context, "div", new ErrorSourceSet( validations: validation?.ToCollection() ), null ),
					classes: TableCssElementCreator.ItemSelectionAndActionContainerClass,
					children: new GenericFlowContainer(
							new GenericPhrasingContainer( "Select:".ToComponents(), classes: TableCssElementCreator.ItemSelectionLabelClass ).Append<FlowComponent>(
									new GenericFlowContainer(
										new WrappingList(
											new EwfButton(
													new StandardButtonStyle( "All", buttonSize: ButtonSize.ShrinkWrap ),
													behavior: new CustomButtonBehavior(
														() => "{0}.find( 'input[type=checkbox]:not(:checked)' ).click();".FormatWith( checkboxCellSelector ) ) )
												.ToComponentListItem()
												.AppendWrappingListItem(
													new EwfButton(
															new StandardButtonStyle( "None", buttonSize: ButtonSize.ShrinkWrap ),
															behavior: new CustomButtonBehavior(
																() => "{0}.find( 'input[type=checkbox]:checked' ).click();".FormatWith( checkboxCellSelector ) ) )
														.ToComponentListItem() ) ).ToCollection(),
										classes: TableCssElementCreator.ItemSelectionControlContainerClass ) )
								.Materialize(),
							classes: TableCssElementCreator.ItemSelectionLabelAndControlContainerClass ).Concat( GetActionList( buttons ) )
						.Materialize() ) ).Append<FlowComponent>(
				new FlowErrorContainer( new ErrorSourceSet( validations: validation?.ToCollection() ), new ListErrorDisplayStyle(), disableFocusabilityOnError: true ) )
			.Materialize();

	internal static IEnumerable<FlowComponent> GetActionList( IReadOnlyCollection<ActionComponentSetup> actions ) {
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

	internal static IEnumerable<IReadOnlyCollection<PhrasingComponent>?>? GetReorderingControls<ItemIdType>(
		string postBackIdBase, bool tableIsColumnPrimary, bool enableItemReordering, bool hasExplicitItemGroups, IReadOnlyList<EwfTableItem<ItemIdType>> items ) {
		if( !enableItemReordering ) {
			if( items.Any( i => i.Setup.RankId.HasValue ) )
				throw new ApplicationException( "Item rank IDs are valid only when item reordering is enabled." );
			return null;
		}

		if( !hasExplicitItemGroups && items.Any( i => !i.Setup.RankId.HasValue ) )
			throw new ApplicationException( "Every item must have a rank ID when item reordering is enabled." );

		if( items.All( i => !i.Setup.RankId.HasValue ) )
			return Enumerable.Repeat( (IReadOnlyCollection<PhrasingComponent>?)null, items.Count );

		if( items.Any( i => !i.Setup.RankId.HasValue ) )
			throw new ApplicationException(
				"When item reordering is enabled, every item in a group must have a rank ID unless none of the items in that group have a rank ID." );

		return items.Select(
			( item, index ) => {
				var components = new List<PhrasingComponent>();
				if( index != 0 )
					components.Add(
						new EwfButton(
							new CustomButtonStyle(
								classes: new ElementClass( "icon" ),
								attributes: new ElementAttribute( "aria-label", "Move up" ).ToCollection(),
								children: new FontAwesomeIcon( tableIsColumnPrimary ? "fa-chevron-circle-left" : "fa-chevron-circle-up", "fa-lg" ).ToCollection() ),
							behavior: new PostBackBehavior(
								postBack: PostBack.CreateFull(
									id: PostBack.GetCompositeId( postBackIdBase, item.Setup.RankId!.Value.ToString(), "up" ),
									modificationMethod: () => RankingMethods.SwapRanks( items[ index - 1 ].Setup.RankId!.Value, item.Setup.RankId.Value ) ) ) ) );
				if( index != 0 && index != items.Count - 1 )
					components.AddRange( " ".ToComponents() );
				if( index != items.Count - 1 )
					components.Add(
						new EwfButton(
							new CustomButtonStyle(
								classes: new ElementClass( "icon" ),
								attributes: new ElementAttribute( "aria-label", "Move down" ).ToCollection(),
								children: new FontAwesomeIcon( tableIsColumnPrimary ? "fa-chevron-circle-right" : "fa-chevron-circle-down", "fa-lg" ).ToCollection() ),
							behavior: new PostBackBehavior(
								postBack: PostBack.CreateFull(
									id: PostBack.GetCompositeId( postBackIdBase, item.Setup.RankId!.Value.ToString(), "down" ),
									modificationMethod: () => RankingMethods.SwapRanks( item.Setup.RankId.Value, items[ index + 1 ].Setup.RankId!.Value ) ) ) ) );
				return components;
			} );
	}

	internal static List<List<CellPlaceholder?>> BuildCellPlaceholderListsForItems(
		IReadOnlyCollection<IReadOnlyCollection<EwfTableCell>> items, int fieldCount ) {
		var itemIndex = 0;
		var cellPlaceholderListsForItems = new List<List<CellPlaceholder?>>();
		foreach( var itemCells in items ) {
			// Add a list of cell placeholders for this item if necessary.
			if( itemIndex >= cellPlaceholderListsForItems.Count )
				addCellPlaceholderListForItem( cellPlaceholderListsForItems, fieldCount );

			var cellPlaceholdersForItem = cellPlaceholderListsForItems[ itemIndex ];

			// Sum the cells taken up by previous items, which can happen when cells have item span values greater than one.
			var potentialCellPlaceholderCountForItem = cellPlaceholdersForItem.Count( cellPlaceholder => cellPlaceholder is not null );

			// Add to that the number of cells this item will take up.
			potentialCellPlaceholderCountForItem += itemCells.Sum( cell => cell.Setup.FieldSpan );

			if( potentialCellPlaceholderCountForItem != fieldCount )
				throw new ApplicationException( "Item to be added has " + potentialCellPlaceholderCountForItem + " cells, but should have " + fieldCount + " cells." );

			// Add this item's cells and any necessary spaces to cellPlaceholderListsForItems.
			var fieldIndex = 0;
			foreach( var cell in itemCells ) {
				while( cellPlaceholdersForItem[ fieldIndex ] != null )
					fieldIndex += 1;
				for( var fieldSpanIndex = 0; fieldSpanIndex < cell.Setup.FieldSpan; fieldSpanIndex += 1 ) {
					for( var itemSpanIndex = 0; itemSpanIndex < cell.Setup.ItemSpan; itemSpanIndex += 1 ) {
						if( itemIndex + itemSpanIndex >= cellPlaceholderListsForItems.Count )
							addCellPlaceholderListForItem( cellPlaceholderListsForItems, fieldCount );
						if( cellPlaceholderListsForItems[ itemIndex + itemSpanIndex ][ fieldIndex ] != null )
							throw new ApplicationException( "Two cells spanning multiple fields and/or items have overlapped." );
						cellPlaceholderListsForItems[ itemIndex + itemSpanIndex ][ fieldIndex ] = itemSpanIndex == 0 && fieldSpanIndex == 0
							                                                                          ? cell
							                                                                          : new SpaceForMultiColOrRowCell();
					}
					fieldIndex += 1;
				}
			}

			itemIndex += 1;
		}

		if( cellPlaceholderListsForItems.Count != items.Count )
			// Since every item must have at least one cell, this message assumes that the first count above is never less than the second.
			throw new ApplicationException( "A cell has overflowed the table." );

		return cellPlaceholderListsForItems;
	}

	private static void addCellPlaceholderListForItem( List<List<CellPlaceholder?>> cellPlaceholderListsForItems, int fieldCount ) {
		var list = new List<CellPlaceholder?>();
		for( var i = 0; i < fieldCount; i++ )
			list.Add( null );
		cellPlaceholderListsForItems.Add( list );
	}

	internal static IEnumerable<FlowComponent> BuildRows(
		List<List<CellPlaceholder?>> cellPlaceholderListsForRows, IReadOnlyList<EwfTableFieldOrItemSetup> rowSetups, bool? useContrastForFirstRow,
		IReadOnlyList<EwfTableFieldOrItemSetup> columns, int firstDataColumnIndex, bool tableIsColumnPrimary ) {
		return cellPlaceholderListsForRows.Select(
			( row, rowIndex ) => {
				var rowSetup = rowSetups[ rowIndex ];
				var rowActivationBehavior = rowSetup.ActivationBehavior;
				return new FlowIdContainer(
					ElementActivationBehavior.GetActivatableElement(
							"tr",
							( useContrastForFirstRow.HasValue && ( ( rowIndex % 2 == 1 ) ^ useContrastForFirstRow.Value )
								  ? TableCssElementCreator.ContrastClass
								  : ElementClassSet.Empty ).Add( rowSetup.Classes ),
							rowSetup.Size != null
								? new ElementAttribute( "style", "height: {0}".FormatWith( rowSetup.Size.Value ) ).ToCollection()
								: Enumerable.Empty<ElementAttribute>().Materialize(),
							rowActivationBehavior,
							row.Select( ( cell, colIndex ) => new { Cell = cell as EwfTableCell, ColumnIndex = colIndex } )
								.Where( cellAndIndex => cellAndIndex.Cell is not null )
								.Select(
									cellAndIndex => {
										var columnSetup = columns[ cellAndIndex.ColumnIndex ];
										var cellSetup = cellAndIndex.Cell!.Setup;

										var attributes = new List<ElementAttribute>();
										var rowSpan = tableIsColumnPrimary ? cellSetup.FieldSpan : cellSetup.ItemSpan;
										if( rowSpan != 1 )
											attributes.Add( new ElementAttribute( "rowspan", rowSpan.ToString() ) );
										var colSpan = tableIsColumnPrimary ? cellSetup.ItemSpan : cellSetup.FieldSpan;
										if( colSpan != 1 )
											attributes.Add( new ElementAttribute( "colspan", colSpan.ToString() ) );

										var cellActivationBehavior = cellSetup.ActivationBehavior ??
										                             ( tableIsColumnPrimary || rowActivationBehavior == null ? columnSetup.ActivationBehavior : null );
										return new FlowIdContainer(
											ElementActivationBehavior.GetActivatableElement(
													cellAndIndex.ColumnIndex < firstDataColumnIndex ? "th" : "td",
													TableCssElementCreator.AllCellAlignmentsClass.Add( textAlignmentClass( cellAndIndex.Cell, rowSetup, columnSetup ) )
														.Add( verticalAlignmentClass( rowSetup, columnSetup ) )
														.Add( cellSetup.ContainsActivatableElements ? activatableElementContainerClass : ElementClassSet.Empty )
														.Add( columnSetup.Classes )
														.Add( cellSetup.Classes ),
													attributes,
													cellActivationBehavior,
													cellAndIndex.Cell.Content,
													cellSetup.EtherealContent )
												.ToCollection(),
											updateRegionSets: cellSetup.UpdateRegionSets );
									} )
								.Materialize(),
							Enumerable.Empty<EtherealComponent>().Materialize() )
						.ToCollection() );
			} );
	}

	private static ElementClassSet textAlignmentClass( EwfTableCell cell, EwfTableFieldOrItemSetup row, EwfTableFieldOrItemSetup column ) {
		// NOTE: Think about whether the row or the column should win if nothing is specified on the cell.
		var alignments = new TextAlignment?[] { cell.Setup.TextAlignment, row.TextAlignment, column.TextAlignment };
		return TextAlignmentStatics.Class( alignments.FirstOrDefault( i => i != TextAlignment.NotSpecified ) ?? TextAlignment.NotSpecified );
	}

	private static ElementClassSet verticalAlignmentClass( EwfTableFieldOrItemSetup row, EwfTableFieldOrItemSetup column ) {
		// NOTE: Think about whether the row or the column should win.
		var alignments = new TableCellVerticalAlignment?[] { row.VerticalAlignment, column.VerticalAlignment };
		return TableCellVerticalAlignmentOps.Class(
			alignments.FirstOrDefault( i => i != TableCellVerticalAlignment.NotSpecified ) ?? TableCellVerticalAlignment.NotSpecified );
	}

	internal static Action<ExcelWorksheet> GetExcelRowAdder( bool rowIsHeader, IReadOnlyCollection<EwfTableCell> cells ) =>
		worksheet => {
			if( cells.Any( i => i.Setup.FieldSpan != 1 || i.Setup.ItemSpan != 1 ) )
				throw new ApplicationException( "Export to Excel does not currently support cells that span multiple columns or rows." );

			if( rowIsHeader )
				worksheet.AddHeaderToWorksheet( cells.Select( i => ( (CellPlaceholder)i ).SimpleText ).ToArray() );
			else
				worksheet.AddRowToWorksheet( cells.Select( i => ( (CellPlaceholder)i ).SimpleText ).ToArray() );
		};

	internal static void AssertAtLeastOneCellPerField( int fieldCount, List<List<CellPlaceholder>> cellPlaceholderListsForItems ) {
		// If there is absolutely nothing in the table, we must bypass the assertion since it will always throw an exception.
		if( !cellPlaceholderListsForItems.Any() )
			return;

		// Enforce that there is at least one cell in each field by looking at array of all items.
		for( var fieldIndex = 0; fieldIndex < fieldCount; fieldIndex += 1 )
			if( !cellPlaceholderListsForItems.Select( i => i[ fieldIndex ] ).OfType<EwfTableCell>().Any() )
				throw new ApplicationException( "The field with index " + fieldIndex + " does not have any cells." );
	}
}