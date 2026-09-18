---
name: ewl-data-access
description: EWL generated data access layer including table retrievals, modifications, small tables, row constants, table constants, sequences, and caching
---

## Overview

The Development Utility generates data-access classes from your database
schema. These live in `Generated Code\` folders and must never be edited
directly. You can extend them with hand-written `partial class` files.

Run `sync` after schema or generation-configuration changes to regenerate.
If the system uses `WhitelistedTables`, include each table before using its
generated APIs. EWL rejects nullable string columns; check compatibility
before promising a table-row replacement in a legacy migration.

## Table retrievals

Generated `*TableRetrieval` classes provide typed read access:

```csharp
// Get all rows
var rows = ServiceOrdersTableRetrieval.GetRows();

// Get rows matching a condition
var rows = ServiceOrdersTableRetrieval.GetRows(
	new ServiceOrdersTableEqualityConditions.CustomerId( customerId ) );

// Get a single row by primary key (throws if not found)
var row = ServiceOrdersTableRetrieval.GetRowMatchingId( serviceOrderId );

// Try to get a single row (returns false if not found)
if( ServiceOrdersTableRetrieval.TryGetRowMatchingId( id, out var row ) )
	// use row

// For tables declared in SmallTables
var allRows = ServiceTypesTableRetrieval.GetAllRows();
```

Row objects expose typed properties for each column (e.g. `row.CustomerName`,
`row.ServiceTypeId`).

Single-column primary keys whose names end in `Id` generate
`GetRowMatchingId`/`TryGetRowMatchingId`; other keys generate
`GetRowMatchingPk`/`TryGetRowMatchingPk`. Inspect generated output for the
actual signatures.

When migrating, prefer these rows over custom records that copy columns and
add lookup names. Put derived properties and related-row accessors in
hand-written partial `Row` classes. Separate types remain appropriate for
distinct concepts, aggregations, and external contracts. Preserve access
checks, null/default semantics, trimming, and ordering during replacement.

## Small tables

Declare genuinely small tables (typically lookup/configuration tables) in
`Development.xml`:

```xml
<SmallTables>
	<Table>UserRoles</Table>
	<Table>ServiceTypes</Table>
</SmallTables>
```

- Use `GetAllRows()` and filter/order in C# for these tables.
- Single-row ID/PK lookups load the full table and use its key cache, so
  repeated related-row accessors reuse the loaded rows while the current
  data-access cache is enabled. This is useful for resolving lookup names
  from many parent rows without one query per lookup ID.
- SQL-condition retrieval is named `GetRowsMatchingConditions(...)` rather
  than `GetRows(...)`. Use it only when filtering in code is unsuitable.
  Declaring an existing table small therefore requires updating callers.
- `SmallTables` alone does **not** create an application-wide cache. Its
  retrieval cache belongs to `DataAccessState.Current` and is reused only
  while caching is enabled. Do not introduce static row caches or modify
  data inside `ExecuteWithCache`; follow the framework’s cache lifecycle.
- Do not mark large user/application/transaction tables small merely to
  obtain convenient APIs. Consider row count and payload size, including
  large text columns.

Small tables and row constants are independent options: a mutable template
table may be small without being a row-constant table; a stable lookup can
use both. Application-level modification-table caching is a separate
mechanism described below.

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

Follow the system’s primary-key convention. For EWL-owned integer IDs this
is normally the main sequence, rather than auto-increment:

```csharp
mod.ServiceOrderId = MainSequence.GetNextValue();
```

Preserve externally assigned IDs and established GUID/legacy identity
conventions; do not substitute a sequence for those keys.

## Row constants

Stable lookup tables declared in `Development.xml` as `rowConstantTables`
produce classes in `DataAccess.RowConstants`, normally named `<Table>Rows`.
They contain values copied from the development database **at generation
time**, not live row retrievals:

```csharp
UserRolesRows.Administrator
ServiceTypesRows.GeneralService
EmailTemplatesRows.Reminder
```

Configure in `Development.xml`:

```xml
<database>
	<rowConstantTables>
		<table tableName="UserRoles" nameColumn="RoleName" valueColumn="UserRoleId" orderByColumn="UserRoleId" />
	</rowConstantTables>
	<SmallTables>
		<Table>UserRoles</Table>
	</SmallTables>
</database>
```

- `nameColumn` supplies C# member names and display names; `valueColumn`
  supplies their typed values. Prefer these constants over magic IDs or
  hand-maintained enums that simply duplicate lookup rows.
- Values are `const` where C# permits (and can be used in switches and
  constant patterns); values requiring construction are `static readonly`.
- `GetNameFromValue(value)` and `GetValueFromName(name)` use the generated
  mapping, not current database contents. Names are copied as stored;
  fixed-width SQL strings can include padding, so trim for display when
  appropriate.
- Specifying `orderByColumn` also generates `GetValuesToNames()` and
  `GetListItems()`, in that order. Without it those list APIs are not
  generated. Inspect output for exact identifiers and value types.
- Keep lookup IDs/names consistent across installations through migrations
  and regenerate after changing them. Runtime-editable names should usually
  be read through retrieval rows instead of treated as generated constants.
- Add the table to `WhitelistedTables` if a whitelist exists. Declaring it
  in `rowConstantTables` does not implicitly whitelist it or make it small.

## Table constants

For every included table, EWL automatically generates schema constants in
`DataAccess.TableConstants`; no separate `tableConstantTables` setting is
needed:

```csharp
ServiceOrdersTable.Name
ServiceOrdersTable.CustomerNameColumn.Name
ServiceOrdersTable.CustomerNameColumn.Size
```

`<Table>Table.Name` is the database-qualified table name. Each non-rowversion
column has a nested `<Column>Column` class with `Name` and `Size` constants.
For character columns, `Size` is the storage length used for string limits;
it is not a row count or a business-rule limit. Prefer these over repeated
schema-name strings and hard-coded maximum lengths when schema metadata is
needed. Generated form items already use schema metadata.

These describe **schema**, whereas row constants describe selected **data**.
They do not replace generated retrieval/modification APIs or parameterized
SQL values. Secondary databases and nondefault schemas affect namespaces;
inspect the actual generated output rather than assuming a namespace.

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

For retrievals, add related-row accessors and computed properties directly
to the partial `Row` class:

```csharp
partial class ServiceOrdersTableRetrieval {
	partial class Row {
		public string ServiceTypeName => ServiceTypesTableRetrieval.GetRowMatchingId( ServiceTypeId ).Name;
	}
}
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

## Framework source references

When a detail or API differs from these examples, inspect generated output
and the EWL source:

- `Development Utility/CodeGeneration/DataAccess/DataAccessOps.cs` —
  whitelist and generation pipeline.
- `Development Utility/CodeGeneration/DataAccess/Subsystems/TableRetrievalStatics.cs`
  — small-table APIs and retrieval caching.
- `Development Utility/CodeGeneration/DataAccess/Subsystems/RowConstantStatics.cs`
  — row-constant identifiers, mappings, and ordered list APIs.
- `Development Utility/CodeGeneration/DataAccess/Subsystems/TableConstantStatics.cs`
  — schema constants.
- `Core/DataAccess/DataAccessState.cs` — cache scope and read-only cache use.
