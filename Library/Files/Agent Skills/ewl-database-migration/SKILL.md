---
name: ewl-database-migration
description: Database schema migration using FluentMigrator with the EWL Data Migrator project
---

## Overview

EWL uses FluentMigrator for database schema and reference data migrations.
Migrations run automatically during `Update-DependentLogic` (for local
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

## Constraint naming conventions

| Constraint | Pattern | Example |
|---|---|---|
| Primary key | `<TableName>Pk` | `RoomsPk` |
| Foreign key | `<TableName><ColumnName>Fk` | `RoomsBuildingIdFk` |
| Unique | `<TableName><ColumnName>Unique` | `RoomsRoomNameUnique` |

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

## Running migrations

Migrations run automatically when you execute `Update-DependentLogic`. Each
installation tracks which migrations have already been applied. When you
deploy to a server, only new migrations run.

If you do not use the EWL System Manager, call `DataMigrator.exe` as part
of your deployment process.

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
and query with Dapper (add a `Dapper` package reference to the Data Migrator
project):

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

## Initial database setup

For initial schema and reference data (before FluentMigrator), use
`Library/Configuration/Database Updates.sql`. This script runs during the
first `Update-DependentLogic` and creates the baseline schema.

## After schema changes

After any migration that changes the schema, run `Update-DependentLogic` to
regenerate the data-access layer (`Generated Code\` files). This keeps
your C# retrieval and modification classes in sync with the database.
