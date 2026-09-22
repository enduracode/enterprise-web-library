---
name: ewl-data-access
description: EWL data access including generated table and custom retrievals, modifications, SprocExecution, small tables, row constants, sequences, and transaction-scoped caching
---

## Overview

The Development Utility generates data-access classes from your database
schema. These live in `Generated Code\` folders and must never be edited
directly. You can extend them with hand-written `partial class` files.

Run `sync` after schema or generation-configuration changes to regenerate.
If the system uses `WhitelistedTables`, include each table before using its
generated APIs. EWL rejects nullable string columns; check compatibility
before promising a table-row replacement in a legacy migration.

Keep declaration lists in `Development.xml` generally alphabetical by
normalized domain name: whitelisted tables, small tables, row-constant tables,
custom queries and their named variants, and custom modifications. Compare
entity words rather than sorting literal identifiers:

- Ignore recognized legacy Hungarian-style prefixes, such as `t` or `tbl`
  on tables and equivalent conventional prefixes on stored procedures or
  other entities. Strip only actual naming prefixes, not letters belonging
  to the domain word. EWL convention is to omit these prefixes on new
  entities, so interleave prefixed and unprefixed names: `UserRequests`
  precedes `tblUserRoles`, and `tblApplicationStatus` precedes `Blobs`.
- Treat established abbreviations as their full entity word. For example,
  `AppType` and `ApplicationStatus` share the base word `Application`;
  compare `Status` with `Type`, placing `ApplicationStatus` first.
- Ignore plural inflections when comparing entity words, placing the base
  entity before its compounds. Thus `Files`, `FileCollections`, and
  `FileCollectionFiles` belong in that order; `Blobs` precedes
  `BlobReferences`. Apply this to compound words as well as whole names.

These are ordering rules, not instructions to rename existing entities.
Allow minor exceptions for sensible domain groupings. Preserve semantically
significant ordering, especially the sequence of SQL commands within a
modification.

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

## Custom retrievals

When table retrievals do not express a join, projection, aggregate, or legacy
schema query, prefer a configured custom retrieval over handwritten command
factories, parameter binding, reader loops, and ordinal-to-model mappings.
These are first-class EWL data access, not an exception requiring raw ADO.NET.

Strongly prefer simple retrievals. Start with generated table retrievals;
when custom SQL is needed, keep it focused on the entity being retrieved.
Do not join small lookup tables merely to obtain names or other related-row
values. Resolve those through cached small-table retrievals in hand-written
partial-row properties, or use row constants when generation-time values are
appropriate. Joins and subqueries remain appropriate for genuine relational
filtering, authorization, revision selection, and aggregation. Preserve their
semantics when simplifying, including any exclusion of missing lookup rows.

For a single-table custom retrieval with no joins, strongly prefer `SELECT *`.
Use explicit projections only for a concrete reason, such as aggregation,
avoiding a materially large payload (e.g. blob content), or a distinct result
contract. Prefer variants of one entity retrieval over separate ID-only and
column-only retrievals when the same row type will serve the callers. Do not
add lookup joins or duplicate DTOs merely to reproduce an old display model.

Declare queries under the database's `queries` in `Development.xml`:

```xml
<queries>
	<query name="Orders">
		<selectFromClause>SELECT * FROM Orders</selectFromClause>
		<postSelectFromClauses>
			<postSelectFromClause name="LinkedToCustomerOrderedById">WHERE CustomerId = @customerId ORDER BY OrderId</postSelectFromClause>
			<postSelectFromClause name="MatchingId">WHERE OrderId = @orderId</postSelectFromClause>
		</postSelectFromClauses>
	</query>
</queries>
```

After generation, this produces `DataAccess.Retrieval.OrdersRetrieval`
with a typed nested `Row`, `GetRowsLinkedToCustomerOrderedById(...)`, and
`GetRowsMatchingId(...)`. Named parameters are
bound by generated code; inspect their generated order and signatures. The
current generator declares query parameters as `object?`, not schema-inferred
strongly typed parameters. Use explicit aliases for expressions and duplicate
column names. Preserve authorization and relational semantics, null/default behavior,
trimming, ordering, and duplicate/single-row expectations.

The table whitelist controls table APIs; it does not prevent a custom query
from referencing other tables. A nullable-string blocker for whole-table
generation does not justify bypassing custom retrievals. Query generation
examines the selected result columns and does not apply the table generator's
nullable-string rejection. Generated query rows map SQL NULL strings to empty
strings; nullable value types remain nullable according to result metadata.
Check whether that represents the intended semantics, especially for outer
joins. Do not expand a migration into schema changes merely to retrieve a
custom retrieval, including `SELECT *` where its null semantics are suitable.

Prefer generated query rows directly where they represent the needed data.
Partial row extensions can add derived properties and lookup names. An existing
record that merely copies columns and lookup labels is not a distinct contract:
replace it with the generated `Row`, and use its column names directly rather
than adding aliases to mimic the record. A genuinely distinct external contract
may justify a projection; it does not require manual SQL execution or ordinal
readers.

Generated methods materialize results and cache them by query variant and
parameter values while the current data-access cache is enabled. Apply the
transaction and cache rules below, rather than bypassing generated retrievals
to attempt a newer read within the same transaction.

### Custom retrieval naming

Use these preferred conventions, drawn from Scheduling Engine, RLE Link,
System Manager, and Todd (TBG Enterprise System). Historical names vary; do
not treat every existing spelling as a rule.

- Name the query for the plural domain entities it returns: `EventExports`,
  `Builds`, `PersonValidationErrors`, `Articles`, `ClientGroups`. The generator
  adds `Retrieval`; omit that suffix and UI-oriented suffixes such as `List`
  from the query name. For example, `Applications` generates
  `ApplicationsRetrieval.Row`.
- Qualify genuinely distinct result shapes or relationships:
  `EventCountsByOrganizationId`, `CountriesWithPersonCount`, `TimeEntryTotals`,
  `ClientsToCarriers`. An `Ids` suffix is appropriate when a deliberately
  ID-only projection is justified, not a reason to create one unnecessarily.
- Put filtering and ordering in `postSelectFromClause` names, which become
  `GetRows<Variant>` methods. Prefer `MatchingId` or `MatchingEmailAddress` for
  value matches and `LinkedToOrganization` or `LinkedToClientGroup` for related
  entities. Describe the selection, rather than just its screen or caller.
- Compose conditions as needed: `MatchingEmailAddressAndActive` or
  `LinkedToPersonNotDeletedOrderedById`. Make important ordering explicit
  with `OrderedBy...` and `Desc` where appropriate, as in System Manager’s
  `LinkedToSystemAndReleasedOrderedByReleaseDateDesc`. Use `Unordered` when
  order is unspecified, especially for multirow variants. A unique-ID match
  does not need an ordering suffix.
- Keep variants returning the same entity shape together under one query.
  Add hand-written helpers and related-row properties in
  `DataAccess/Retrieval/<QueryName>Retrieval.cs` as partial classes; keep
  workflow coordination in domain classes such as `ApplicationStatics`.

Reference declarations are in each system’s `Library/Configuration/Development.xml`:
Scheduling Engine (`EventExports`, `EventCountsByOrganizationId`), RLE Link
(`PersonValidationErrors`, `CountriesWithPersonCount`), System Manager
(`Builds`, `ChangeLocations`), and Todd (`Articles`, `ClientGroups`,
`ClientsToCarriers`). These are naming examples, not blanket endorsements of
the complexity of their SQL.

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

### Modification timestamps

Use the timestamp for the logical operation so related modifications in the same
transaction receive the same value:

| Code context | Default timestamp |
|---|---|
| Web-specific code | `EwfRequest.Current.RequestTime` |
| Background-service code | `BackgroundServiceStatics.TickTime` |
| Shared Library code callable from multiple application types | `Clock.TransactionTime` |

Prefer these over repeated reads of `SystemClock.Instance.GetCurrentInstant()`,
`DateTime.UtcNow`, or other current-time APIs. There must be a strong,
task-specific reason to use current wall-clock time instead, such as an explicit
requirement to record the actual completion instant of an external operation.
Explain that exception; a potentially long request or tick alone does not
override the default. When changing a status and recording its timestamp, assign
both in the same database modification.

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

For SQL writes not covered by table modifications, use configured
`customModifications` rather than a generic handwritten command factory:

```xml
<customModifications>
	<modification>
		<name>DeleteOrderNotes</name>
		<commands>
			<command>DELETE FROM OrderNotes WHERE OrderId = @orderId</command>
		</commands>
	</modification>
</customModifications>
```

This generates `DataAccess.CustomModifications.DeleteOrderNotes(...)`, binds
named parameters, and executes the configured commands in a transaction.
Nested transaction execution does not commit the outer request transaction.
Use this for custom SQL writes; stored procedures can be called directly as
described next, without adding generation configuration.

## Stored procedures

Use `SprocExecution` from
`EnterpriseWebLibrary.DataAccess.CommandWriting.Commands` directly. Its
`ExecuteNonQuery`, `ExecuteScalar`, and `ExecuteReader` methods use an EWL
`DatabaseConnection` and therefore its current transaction.

```csharp
var command = new SprocExecution( "AddApplication" );
command.AddParameter( new DbCommandParameter( "ApplicationID", new DbParameterValue( applicationId, "UniqueIdentifier" ) ) );
command.AddParameter( new DbCommandParameter( "UserID", new DbParameterValue( userId, "Int" ) ) );
command.ExecuteNonQuery( DataAccessState.Current.PrimaryDatabaseConnection );
```

`DbCommandParameter` and `DbParameterValue` are in
`EnterpriseWebLibrary.DataAccess.CommandWriting`. The example uses SQL Server
type names; select actual procedure parameter types for the database provider.
Avoid generic string-name/object-tuple wrappers with runtime CLR-to-SQL type
switches: explicit operation-specific calls already use the framework's
first-class procedure API. Inspect the actual procedure contract for return
values, output parameters, nulls, and side effects.

Do not promise generated `Procedures` methods for SQL Server. The current DU
invokes `ProcedureStatics.Generate` only for Oracle. Direct `SprocExecution`
does not require code generation and is appropriate for SQL Server procedures.

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

## Transactions and retrieval caching

EWL's normal data-access model uses a consistent database snapshot for the
unit of work. `AutomaticDatabaseConnectionManager` owns a `DataAccessState`
and lazily opens connections and begins automatic transactions. SQL Server
transactions explicitly use `IsolationLevel.Snapshot`, so the database must
support/enable snapshot isolation; Oracle uses Serializable. Do not confuse
SQL Server SNAPSHOT with statement-level read-committed snapshot isolation.

In web applications, each request has a connection manager and data-access
state. The framework generally manages cache and transaction boundaries:

- Reads during normal request processing share the database transaction's
  snapshot. Re-executing a SELECT, bypassing a retrieval cache, or resetting
  that cache does **not** obtain newer commits from other transactions.
- All modifications belong in the framework's modification phase. Reads,
  validation, and authorization checks do not universally need to be moved
  there for “freshness”; they operate against the same snapshot.
- The framework disables retrieval caching during modifications and resets
  it afterward. This prevents cached pre-modification values from hiding
  the transaction's **own writes**; it does not refresh the database snapshot.
- Entering/exiting the modification phase uses nested transaction/savepoint
  handling, not a new snapshot or an early outer commit.
- The state object's lifetime is coordinated with transactions, but is not
  strictly one object per transaction. It may survive framework-managed
  commit/rollback boundaries, at which the framework resets caches. Multiple
  databases have separate transactions, not one globally atomic snapshot.

Do not add manual `ExecuteWithCache` blocks or ad hoc cache resets to normal
web request handling. Other application types sometimes explicitly create
read-only `DataAccessState.Current.ExecuteWithCache(...)` blocks; those blocks
enable retrieval caching, **not** a database transaction. Coordinate them
with the application's transaction lifetime, exclude modifications, and do
not reuse cached results across transaction boundaries. Nested cache blocks
reuse the existing cache. Where an automatic connection manager is used,
`ExecuteWithModificationsEnabled` provides the write/cache lifecycle.

Stored procedures, raw SQL, and external-process writes do not independently
invalidate arbitrary generated query caches. Use the normal modification
lifecycle rather than inventing a cache invalidation layer. A legacy HTTP
request or supplemental connection has its own transaction: it cannot see
the caller's uncommitted inserts, and its commits do not refresh the caller's
existing snapshot. Treat that as transaction/workflow design, not a reason
to use handwritten retrievals. Snapshot isolation and cached reads do not
by themselves enforce every concurrency invariant; use the appropriate
database constraints and transactional design rather than repeated “fresh”
checks within the same snapshot.

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
- `Development Utility/CodeGeneration/DataAccess/Subsystems/QueryRetrievalStatics.cs`
  and `Development Utility/CodeGeneration/DataAccess/Column.cs` — custom query
  generation, result types, and null handling.
- `Development Utility/CodeGeneration/DataAccess/Subsystems/CustomModificationStatics.cs`
  — configured SQL modifications and transaction wrappers.
- `Core/DataAccess/CommandWriting/Commands/SprocExecution.cs` — direct procedure calls.
- `Core/DataAccess/AutomaticDatabaseConnectionManager.cs` and
  `Core/DataAccess/DatabaseConnection.cs` — state/transaction lifetimes,
  snapshot isolation, and savepoints.
- `Core/EnterpriseWebFramework/Page Infrastructure/Data Modification/BasicDataModificationAction.cs`
  — web modification/cache lifecycle.
