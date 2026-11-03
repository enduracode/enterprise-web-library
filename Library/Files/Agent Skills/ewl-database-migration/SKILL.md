---
name: ewl-database-migration
description: Database schema migration using FluentMigrator with the EWL Data Migrator project
---

## Overview

For PostgreSQL client systems, also read [PostgreSQL migrations](PostgreSQL.md).

EWL uses FluentMigrator for database schema and reference data migrations.
Migrations run automatically during `sync` (for local
development) and during deployment via `DataMigrator.exe`.

## Setting up the Data Migrator

If the `Data Migrator` project is not yet in your solution:

1. Locate the generated `Data Migrator/Data Migrator.ewlt.csproj`
2. Change the extension to just `.csproj`
3. Add the project to your solution
4. Reference the same version of EWL that the other projects use

## Writing migrations

Add migration classes to the `Data Migrator` project. Each migration class
must have a unique, sequential version number.

Rules:

- **Use `ForwardOnlyMigration`**, not `Migration`. Only define `Up()`.
- **Namespace**: `namespace DataMigrator;` — always matches the `RootNamespace`
  in `Directory.Build.props`, not the system namespace.
- **Descriptive class names**: Name the class after the action it performs (e.g.
  `CreateRooms`, `AddRoomsCapacity`, `AddManagerUserRole`). Never use generic
  names like `Migration1`.
- **File naming**: Name each file after its class.
- **Attribute spacing**: `[ Migration( N ) ]` with spaces inside brackets and
  parentheses, per EWL style.
- **Formatting**: Put the `Create.Table()` call and `.InSchema()` (if any) on
  the first line. Put each column's full spec chain (`.WithColumn()` through
  all modifiers) on a single line.

```csharp
using FluentMigrator;

namespace DataMigrator;

[ Migration( 1 ) ]
public class CreateRooms: ForwardOnlyMigration {
	public override void Up() {
		Create.Table( "Rooms" )
			.WithColumn( "RoomId" ).AsInt32().NotNullable().PrimaryKey( "RoomsPk" )
			.WithColumn( "BuildingId" ).AsInt32().NotNullable().ForeignKey( "RoomsBuildingIdFk", "Buildings", "BuildingId" )
			.WithColumn( "RoomName" ).AsString( 200 ).NotNullable()
			.WithColumn( "IsActive" ).AsBoolean().NotNullable();
	}
}
```

## Identifier names

Pass raw, unquoted names to fluent APIs: `.Table( "Orders" )`, not
`.Table( "[Orders]" )`. EWL's migration quoters add delimiters and escape embedded
delimiter characters. SQL Server, MySQL, PostgreSQL, and SQLite preserve the
supplied spelling; Oracle uppercases names before quoting. Oracle does not support
identifier names containing double quotes or case-sensitive lowercase/mixed-case
names under this convention. Preserve each system's naming conventions; MySQL
table-name case sensitivity depends on the server and platform.

Supply schema names separately using `.InSchema(...)` or the appropriate schema
argument, not as part of a dotted table name. SQL Server and PostgreSQL support
separate schemas. FluentMigrator's MySQL provider ignores schema arguments; use
the configured database. SQLite schema names refer to attached-database aliases,
not independently created schemas.

These rules apply to fluent identifier arguments, not SQL text or data values.
In `Execute.Sql`, scripts, and Dapper queries, quote identifiers yourself using the
database's SQL syntax and keep parameter binding for data values. PostgreSQL has
an additional identifier-name limitation described in [PostgreSQL migrations](PostgreSQL.md).

## Automatic constraint and index naming conventions

When a fluent migration omits a name, EWL uses these conventions:

| Object | PostgreSQL/Oracle | SQL Server/MySQL/SQLite |
|---|---|---|
| Primary key (inline or separate) | `rooms_pk` | `RoomsPk` |
| Foreign key | `rooms_building_id_fk` | `RoomsBuildingIdFk` |
| Unique constraint/index | `rooms_room_name_unique` | `RoomsRoomNameUnique` |
| Nonunique index | `rooms_building_id_index` | `RoomsBuildingIdIndex` |

PostgreSQL/Oracle use snake_case; the other providers concatenate original
table/column spelling with the suffix. Oracle's quoter uppercases names.
Composite columns retain declaration order; foreign keys use the referencing
table/columns. Schema names are excluded. Inline `.Unique()` uses the unique-index
convention. Explicit names bypass automatic naming but still pass through quoting.

Use explicit names for collisions, identifier-length limits, unique-index deletion,
and existing objects with older names. Unnamed `Delete.Index()` cannot identify
uniqueness and generates the nonunique index name. Existence checks and raw SQL
require actual names; existing objects are not renamed.

For inline foreign keys on a column definition, use:

```csharp
.ForeignKey( "<Table><Column>Fk", "ReferencedTable", "ReferencedColumn" )
```

When the referenced table is in a different schema, add the schema argument:

```csharp
.ForeignKey( "<Table><Column>Fk", "dbo", "ReferencedTable", "ReferencedColumn" )
```

Prefer inline `.ForeignKey()` on the column over separate
`Create.ForeignKey()` statements for single-column foreign keys.

## Schemas

To create a table in a non-default schema, first create the schema, then
use `.InSchema()`:

```csharp
Create.Schema( "Reporting" );

Create.Table( "MonthlyTotals" ).InSchema( "Reporting" )
	.WithColumn( "MonthlyTotalId" ).AsInt32().NotNullable().PrimaryKey( "MonthlyTotalsPk" )
	.WithColumn( "Amount" ).AsDecimal( 10, 2 ).NotNullable();
```

## FluentMigrator type mappings for SQL Server

Use the built-in FluentMigrator type methods. Avoid `.AsCustom()` when a
proper API method exists.

| FluentMigrator method | SQL Server type |
|---|---|
| `.AsString( n )` | `NVARCHAR(n)` |
| `.AsString( int.MaxValue )` | `NVARCHAR(MAX)` |
| `.AsAnsiString( n )` | `VARCHAR(n)` |
| `.AsAnsiString( int.MaxValue )` | `VARCHAR(MAX)` |
| `.AsInt32()` | `INT` |
| `.AsInt64()` | `BIGINT` |
| `.AsInt16()` | `SMALLINT` |
| `.AsByte()` | `TINYINT` |
| `.AsBoolean()` | `BIT` |
| `.AsDecimal( p, s )` | `DECIMAL(p, s)` |
| `.AsDateTime2()` | `DATETIME2` |
| `.AsDate()` | `DATE` |
| `.AsGuid()` | `UNIQUEIDENTIFIER` |
| `.AsBinary( n )` | `VARBINARY(n)` |
| `.AsCustom( "datetime2( 0 )" )` | `DATETIME2(0)` (precision variants) |

Reserve `.AsCustom()` for types that have no built-in method, such as
`datetime2` with a specific precision.

### Time-column precision

For ordinary SQL Server business timestamps where whole-second precision is
sufficient, prefer `.AsCustom( "datetime2( 0 )" )` over `.AsDateTime2()`.
`datetime2(0)` uses 6 bytes per value, compared with 8 for the default
`datetime2(7)`; `datetime2(3)` uses 7 bytes. Choose higher precision when an
actual requirement calls for it, rather than using the maximum by default.
Examples include `Files.UploadTime` in eMOLST and IAEM CEM, and
`UserEmailVerifications.VerificationTime` and `GlobalRow.SalesforceLastLoginTime`
in Rochester RHIO, all declared with `.AsCustom( "datetime2( 0 )" )`.

## Running migrations

Migrations run automatically when you execute `sync`. Each
installation tracks which migrations have already been applied. When you
deploy to a server, only new migrations run.

If you do not use the EWL System Manager, call `DataMigrator.exe` as part
of your deployment process.

### Command timeouts

EWL sets a 60-minute default command timeout for FluentMigrator-issued
commands and Dapper calls in the standalone Data Migrator. This applies per
command, not to the entire migration. Normally omit Dapper’s `commandTimeout`;
explicit values, specified in seconds, override the default. Direct ADO.NET
commands and bulk-copy APIs require their own timeout configuration.

## Common migration patterns

### Adding a column

```csharp
[ Migration( 2 ) ]
public class AddRoomsCapacity: ForwardOnlyMigration {
	public override void Up() {
		Alter.Table( "Rooms" ).AddColumn( "Capacity" ).AsInt32().Nullable();
	}
}
```

### Adding a NOT NULL column to a table with existing rows

```csharp
[ Migration( 3 ) ]
public class AddRoomsFloor: ForwardOnlyMigration {
	public override void Up() {
		Create.Column( "Floor" ).OnTable( "Rooms" ).AsInt32().NotNullable().SetExistingRowsTo( 1 );
	}
}
```

### Adding reference data

```csharp
[ Migration( 4 ) ]
public class AddManagerUserRole: ForwardOnlyMigration {
	public override void Up() {
		Insert.IntoTable( "UserRoles" ).Row( new { UserRoleId = 3, RoleName = "Manager" } );
	}
}
```

### Complex row-by-row logic

For cursor-type logic that loops through rows, use `Execute.WithConnection`
and query with Dapper. Dapper is supplied transitively by EWL; add `using Dapper;`.

```csharp
Execute.WithConnection( ( connection, transaction ) => {
	var rows = connection.Query( "SELECT Id, OldValue FROM MyTable", transaction: transaction );
	foreach( var row in rows ) {
		connection.Execute(
			"UPDATE MyTable SET NewValue = @val WHERE Id = @id",
			new { val = Transform( row.OldValue ), id = row.Id },
			transaction: transaction );
	}
} );
```

## After schema changes

After any migration that changes the schema, run `sync` to
regenerate the data-access layer (`Generated Code\` files). This keeps
your C# retrieval and modification classes in sync with the database.
