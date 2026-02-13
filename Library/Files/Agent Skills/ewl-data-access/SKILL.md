---
name: ewl-data-access
description: EWL generated data access layer including table retrievals, modifications, row constants, sequences, and caching
---

## Overview

The Development Utility generates data-access classes from your database
schema. These live in `Generated Code\` folders and must never be edited
directly. You can extend them with hand-written `partial class` files.

Run `Update-DependentLogic` after any schema change to regenerate.

## Table retrievals

Generated `*TableRetrieval` classes provide typed read access:

```csharp
// Get all rows
var rows = ServiceOrdersTableRetrieval.GetRows();

// Get rows matching a condition
var rows = ServiceOrdersTableRetrieval.GetRows(
	new ServiceOrdersTableEqualityConditions.CustomerId( customerId ) );

// Get a single row by primary key (throws if not found)
var row = ServiceOrdersTableRetrieval.GetRowMatchingPk( serviceOrderId );

// Try to get a single row (returns false if not found)
if( ServiceOrdersTableRetrieval.TryGetRowMatchingPk( id, out var row ) )
	// use row

// For small/cached tables
var allRows = ServiceTypesTableRetrieval.GetAllRows();
```

Row objects expose typed properties for each column (e.g. `row.CustomerName`,
`row.ServiceTypeId`).

## Modifications

Generated `*Modification` classes provide insert, update, and delete:

```csharp
// Insert
var mod = ServiceOrdersModification.CreateForInsert();
mod.ServiceOrderId = MainSequence.GetNextValue();
mod.CustomerName = "Jane Doe";
mod.CustomerEmail = "jane@example.com";
mod.Execute();

// Update from a condition
var mod = UsersModification.CreateForUpdate(
	new UsersTableEqualityConditions.UserId( userId ) );
mod.EmailAddress = newEmail;
mod.Execute();

// Update from a row (common pattern)
var mod = serviceOrderRow.ToModification();
mod.CustomerName = "Updated Name";
mod.Execute();

// Delete
ServiceOrdersModification.DeleteRows(
	new ServiceOrdersTableEqualityConditions.ServiceOrderId( id ) );

// Insert with all columns in one call
UsersModification.InsertRow( userId, email, roleId, 0, null, null, null, null, null, "" );
```

## Primary keys

All new entity IDs use the main sequence, never auto-increment:

```csharp
mod.ServiceOrderId = MainSequence.GetNextValue();
```

## Row constants

Lookup tables declared in `Development.xml` as `rowConstantTables` produce
generated constant classes:

```csharp
UserRolesRows.Administrator
ServiceTypesRows.GeneralService
EmailTemplatesRows.Reminder
```

Configure in `Development.xml`:

```xml
<database>
	<rowConstantTables>
		<table tableName="UserRoles" nameColumn="RoleName" valueColumn="UserRoleId" />
	</rowConstantTables>
	<SmallTables>
		<Table>UserRoles</Table>
	</SmallTables>
</database>
```

## Form items from modifications

Modification objects generate form controls directly. This is the primary
way forms are built in EWL:

```csharp
mod.GetCustomerNameFormItem( false )
mod.GetCustomerEmailFormItem( false )
mod.GetServiceTypeIdDropDownFormItem( DropDownSetup.Create( items ), "" )
mod.GetNotesFormItem( true, controlSetup: TextControlSetup.Create( numberOfRows: 4 ) )
```

The first boolean parameter controls whether the field is optional. Control
types are inferred from column names (e.g. "Email" columns get email controls).

## Extending generated classes

Create partial classes alongside the `.ewlt.cs` files to add custom logic:

```csharp
partial class UsersModification {
	static partial void populateConstraintNamesToViolationErrorMessages(
			Dictionary<string, string> constraintNamesToViolationErrorMessages ) {
		constraintNamesToViolationErrorMessages.Add(
			"UsersEmailAddressUnique", "A user with this email address already exists." );
	}
}
```

Available partial methods for modifications: `preInsert`, `postInsert`,
`preUpdate`, `postUpdate`, `preDelete`,
`populateConstraintNamesToViolationErrorMessages`.

For retrievals, add extension methods or computed properties on the `Row`
class:

```csharp
public static IEnumerable<Row> Active( this IEnumerable<Row> rows ) =>
	rows.Where( i => i.IsActive );
```

## Application-level table caching

Create a companion `YourDataTableEwlModifications` table containing only the
main table's primary key column(s). This enables change-tracking cache
invalidation so that large queries become tiny queries while never returning
stale data.

Requirements:
- All modifications go through EWL-generated Modification classes
- Database transactions use snapshot isolation

## Revision history

Tables in `revisionHistoryTables` in `Development.xml` get automatic
versioning. Retrieval and modification classes are revision-history-aware:
retrievals return only the latest revision, and modifications automatically
create new revisions.

Custom retrieval queries are NOT revision-history-aware; you must join:

```sql
SELECT s.* FROM SomeTableRevisions s
JOIN Revisions r ON r.RevisionId = s.SomeTableRevisionId
	AND r.LatestRevisionId = r.RevisionId
```
