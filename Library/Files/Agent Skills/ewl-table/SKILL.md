---
name: ewl-table
description: Build data tables with EwfTable including pagination, sorting, row activation, selection, and group actions
---

## Basic table

Create a table with `EwfTable.Create`, add data rows with `AddData`:

```csharp
EwfTable.Create(
		caption: "Service orders",
		fields: new EwfTableField( size: 1.ToPercentage() )
			.Append( new EwfTableField( size: 3.ToPercentage() ) )
			.Append( new EwfTableField( size: 6.ToPercentage() ) )
			.Materialize(),
		headItems: EwfTableItem.Create(
				"ID".ToCell().Append( "Customer".ToCell() ).Append( "Bicycle".ToCell() ).Materialize() )
			.ToCollection(),
		defaultItemLimit: DataRowLimit.Fifty )
	.AddData(
		ServiceOrdersTableRetrieval.GetRows(),
		i => EwfTableItem.Create(
			i.ServiceOrderId.ToString().ToCell()
				.Append( i.CustomerName.ToCell() )
				.Append( i.BicycleDescription.ToCell() )
				.Materialize() ) )
```

## Field sizing

Field sizes use proportions (not required to sum to 100%). The
`.ToPercentage()` call is just a helper:

```csharp
fields: new EwfTableField( size: 1.ToPercentage() )
	.Append( new EwfTableField( size: 3.ToPercentage() ) )
	.Append( new EwfTableField( size: 8.ToPercentage() ) )
	.Materialize()
```

## Row activation

Make rows clickable by adding an `activationBehavior` to the item setup:

```csharp
EwfTableItem.Create(
	cells,
	setup: EwfTableItemSetup.Create(
		activationBehavior: ElementActivationBehavior.CreateHyperlink( new EditPage( i.Id ) ) ) )
```

Check authorization before creating links:

```csharp
var page = new EditPage( i.Id );
setup: EwfTableItemSetup.Create(
	activationBehavior: page.UserCanAccess ? ElementActivationBehavior.CreateHyperlink( page ) : null )
```

## Item limiting

`defaultItemLimit` limits initially visible rows for performance. Users can
show more via built-in buttons:

```csharp
EwfTable.Create( defaultItemLimit: DataRowLimit.Fifty )
```

Available limits: `DataRowLimit.Fifty`, `DataRowLimit.FiveHundred`,
`DataRowLimit.Unlimited`.

## Table actions

Add buttons above the table:

```csharp
EwfTable.Create(
	tableActions: new HyperlinkSetup( new CreatePage(), "Add New" ).ToCollection() )
```

## Head items

Header rows use `EwfTableItem.Create` with cell collections:

```csharp
headItems: EwfTableItem.Create(
		"Name".ToCell().Append( "Email".ToCell() ).Append( "Role".ToCell() ).Materialize() )
	.ToCollection()
```

## Building cells

Use `.ToCell()` to convert strings or component collections to table cells:

```csharp
i.Name.ToCell()
"Active".ToComponents().ToCell()
new EwfHyperlink( target, new StandardHyperlinkStyle( "Edit" ) ).ToCell()
```

## Reorderable rows

For drag-and-drop reordering, pass `rankId` to the item setup:

```csharp
EwfTableItemSetup.Create( rankId: i.OrderRankId )
```

## ColumnPrimaryTable

For transposed tables where items render as columns instead of rows:

```csharp
ColumnPrimaryTable.Create( fields: /* ... */ )
	.AddData( items, i => EwfTableItem.Create( /* cells */ ) )
```

## AddData selector

The `AddData` method takes a data sequence and a selector function. Selectors
only execute for rows that are actually drawn (respecting item limits):

```csharp
.AddData(
	dataRows,
	i => EwfTableItem.Create(
		i.Name.ToCell().Append( i.Email.ToCell() ).Materialize(),
		setup: EwfTableItemSetup.Create( /* ... */ ) ) )
```

## Selectable rows with group actions

For tables with checkboxes and bulk operations, use `selectedItemActions`
and `ItemIdCell`:

```csharp
EwfTable.Create(
	selectedItemActions: new SelectedItemAction<int>( "Delete Selected", ids => { /* delete logic */ } )
		.ToCollection() )
.AddData(
	rows,
	i => EwfTableItem.Create(
		new EwfTableItemSetup( id: i.Id ),
		i.Name.ToCell().Materialize() ) )
```
