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
must have a unique, sequential version number:

```csharp
using FluentMigrator;

namespace MySystem.DataMigrator;

[Migration( 1 )]
public class Migration1: Migration {
	public override void Up() {
		Create.Table( "Rooms" )
			.WithColumn( "RoomId" ).AsInt32().NotNullable().PrimaryKey()
			.WithColumn( "RoomName" ).AsString( 200 ).NotNullable()
			.WithColumn( "IsActive" ).AsBoolean().NotNullable();
	}

	public override void Down() {
		Delete.Table( "Rooms" );
	}
}
```

## Running migrations

Migrations run automatically when you execute `Update-DependentLogic`. Each
installation tracks which migrations have already been applied. When you
deploy to a server, only new migrations run.

If you do not use the EWL System Manager, call `DataMigrator.exe` as part
of your deployment process.

## Common migration patterns

### Adding a column

```csharp
[Migration( 2 )]
public class Migration2: Migration {
	public override void Up() {
		Alter.Table( "Rooms" ).AddColumn( "Capacity" ).AsInt32().Nullable();
	}

	public override void Down() {
		Delete.Column( "Capacity" ).FromTable( "Rooms" );
	}
}
```

### Adding reference data

```csharp
[Migration( 3 )]
public class Migration3: Migration {
	public override void Up() {
		Insert.IntoTable( "UserRoles" ).Row( new { UserRoleId = 3, RoleName = "Manager" } );
	}

	public override void Down() {
		Delete.FromTable( "UserRoles" ).Row( new { UserRoleId = 3 } );
	}
}
```

### Complex row-by-row logic

For cursor-type logic that loops through rows, use `Execute.WithConnection`
and query with Dapper:

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
