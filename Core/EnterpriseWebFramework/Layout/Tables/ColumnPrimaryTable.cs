using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A column-primary table.
	/// </summary>
	public sealed class ColumnPrimaryTable: FlowComponent {
		private readonly IReadOnlyCollection<DisplayableElement> outerChildren;

		/// <summary>
		/// Creates a table with one item group.
		/// </summary>
		/// <param name="displaySetup"></param>
		/// <param name="style">The table's style.</param>
		/// <param name="classes">The classes on the table.</param>
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
		/// <param name="items">The items.</param>
		/// <param name="etherealContent"></param>
		public ColumnPrimaryTable(
			DisplaySetup displaySetup = null, EwfTableStyle style = EwfTableStyle.Standard, ElementClassSet classes = null, string caption = "",
			string subCaption = "", bool allowExportToExcel = false, IReadOnlyCollection<ActionComponentSetup> tableActions = null,
			IReadOnlyCollection<EwfTableField> fields = null, IReadOnlyCollection<EwfTableItem> headItems = null, int firstDataFieldIndex = 0,
			IReadOnlyCollection<EwfTableItem> items = null, IReadOnlyCollection<EtherealComponent> etherealContent = null ): this(
			displaySetup,
			style,
			classes,
			caption,
			subCaption,
			allowExportToExcel,
			tableActions,
			fields,
			headItems,
			firstDataFieldIndex,
			items != null ? new ColumnPrimaryItemGroup( null, items: items ).ToCollection() : null,
			etherealContent ) {}

		/// <summary>
		/// Creates a table with multiple item groups.
		/// </summary>
		/// <param name="displaySetup"></param>
		/// <param name="style">The table's style.</param>
		/// <param name="classes">The classes on the table.</param>
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
		/// <param name="itemGroups">The item groups.</param>
		/// <param name="etherealContent"></param>
		public ColumnPrimaryTable(
			DisplaySetup displaySetup = null, EwfTableStyle style = EwfTableStyle.Standard, ElementClassSet classes = null, string caption = "",
			string subCaption = "", bool allowExportToExcel = false, IReadOnlyCollection<ActionComponentSetup> tableActions = null,
			IReadOnlyCollection<EwfTableField> fields = null, IReadOnlyCollection<EwfTableItem> headItems = null, int firstDataFieldIndex = 0,
			IReadOnlyCollection<ColumnPrimaryItemGroup> itemGroups = null, IReadOnlyCollection<EtherealComponent> etherealContent = null ) {
			tableActions = tableActions ?? Enumerable.Empty<ActionComponentSetup>().Materialize();

			if( fields != null && !fields.Any() )
				throw new ApplicationException( "If fields are specified, there must be at least one of them." );

			headItems = headItems ?? Enumerable.Empty<EwfTableItem>().Materialize();
			itemGroups = itemGroups ?? Enumerable.Empty<ColumnPrimaryItemGroup>().Materialize();

			outerChildren = new DisplayableElement(
				tableContext => {
					var children = new List<FlowComponentOrNode>();

					children.AddRange( EwfTable.GetCaption( caption, subCaption ) );

					var itemSetupLists = new[] { headItems }.Concat( itemGroups.Select( i => i.Items ) )
						.Select( i => i.Select( j => j.Setup.FieldOrItemSetup ) )
						.Materialize();
					var allItemSetups = itemSetupLists.SelectMany( i => i ).ToImmutableArray();
					var columnWidthFactor = EwfTable.GetColumnWidthFactor( allItemSetups );
					foreach( var itemSetups in itemSetupLists.Where( i => i.Any() ) ) {
						children.Add(
							new ElementComponent(
								context => new ElementData(
									() => new ElementLocalData( "colgroup" ),
									children: itemSetups.Select( i => EwfTable.GetColElement( i, columnWidthFactor ) ).Materialize() ) ) );
					}

					var tableLevelGeneralActionList = EwfTable.GetGeneralActionList( allowExportToExcel, tableActions ).Materialize();
					if( tableLevelGeneralActionList.Any() ) {
						// NOTE: Table-level item actions, the group head, group-level item actions, and the checkbox row should all go here.
						var tHeadRows = new EwfTableItem(
							new GenericFlowContainer( tableLevelGeneralActionList, classes: EwfTable.ItemLimitingAndGeneralActionContainerClass ).ToCell(
								new TableCellSetup( fieldSpan: allItemSetups.Length ) ) ).ToCollection();
						var cellPlaceholderListsForTHeadRows = TableOps.BuildCellPlaceholderListsForItems( tHeadRows, allItemSetups.Length );
						children.Add(
							new ElementComponent(
								context => new ElementData(
									() => new ElementLocalData( "thead" ),
									children: TableOps.BuildRows(
											cellPlaceholderListsForTHeadRows,
											tHeadRows.Select( i => i.Setup.FieldOrItemSetup ).ToImmutableArray(),
											null,
											Enumerable.Repeat( new EwfTableField().FieldOrItemSetup, allItemSetups.Length ).ToImmutableArray(),
											0,
											false )
										.Materialize() ) ) );
					}

					fields = EwfTable.GetFields( fields, headItems, itemGroups.SelectMany( i => i.Items ) );
					var cellPlaceholderListsForItems = TableOps.BuildCellPlaceholderListsForItems(
						headItems.Concat( itemGroups.SelectMany( i => i.Items ) ).ToList(),
						fields.Count );

					// Pivot the cell placeholders from column primary into row primary format.
					var cellPlaceholderListsForRows = Enumerable.Range( 0, fields.Count )
						.Select( field => Enumerable.Range( 0, allItemSetups.Length ).Select( item => cellPlaceholderListsForItems[ item ][ field ] ).ToList() )
						.ToList();

					var headRows = TableOps.BuildRows(
						cellPlaceholderListsForRows.Take( firstDataFieldIndex ).ToList(),
						fields.Select( i => i.FieldOrItemSetup ).ToImmutableArray(),
						null,
						allItemSetups,
						allItemSetups.Length,
						true );
					var bodyRows = TableOps.BuildRows(
						cellPlaceholderListsForRows.Skip( firstDataFieldIndex ).ToList(),
						fields.Select( i => i.FieldOrItemSetup ).ToImmutableArray(),
						false,
						allItemSetups,
						headItems.Count,
						true );

					// We can't easily put the head fields in thead because we don't have a way of verifying that cells don't cross between head and data fields.
					children.Add(
						new ElementComponent( context => new ElementData( () => new ElementLocalData( "tbody" ), children: headRows.Concat( bodyRows ).Materialize() ) ) );

					EwfTable.AssertAtLeastOneCellPerField( fields, cellPlaceholderListsForItems );

					return new DisplayableElementData(
						displaySetup,
						() => new DisplayableElementLocalData( "table" ),
						classes: EwfTable.GetClasses( style, classes ?? ElementClassSet.Empty ),
						children: children,
						etherealChildren: etherealContent );
				} ).ToCollection();
		}

		IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() => outerChildren;
	}
}