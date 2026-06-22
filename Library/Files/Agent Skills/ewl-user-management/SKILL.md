---
name: ewl-user-management
description: Set up EWL user management with database schema, roles, identity providers, and the SystemUserManagementProvider
---

## Overview

EWL provides a built-in user management system with roles, authentication
(local login codes or SAML), and a framework-provided admin UI. To use it
you need database schema, a provider implementation, and configuration.

## Database schema

Create the required tables. For SQL Server:

```sql
create table UserRoles(
	UserRoleId int not null constraint UserRolesPk primary key,
	RoleName varchar( 100 ) not null
)
go

insert into UserRoles values( 1, 'Administrator' )
insert into UserRoles values( 2, 'Standard user' )
go

create table Users(
	UserId int not null constraint UsersPk primary key,
	EmailAddress varchar( 100 ) not null constraint UsersEmailAddressUnique unique,
	RoleId int not null constraint UsersRoleIdFk references UserRoles,
	Salt int not null,
	SaltedPassword varbinary( 20 ) null,
	LoginCodeSalt varbinary( 16 ) null,
	HashedLoginCode varbinary( 20 ) null,
	LoginCodeExpirationTime datetime2 null,
	LoginCodeRemainingAttemptCount tinyint null,
	LoginCodeDestinationUrl varchar( 500 ) not null
)
go

insert into Users values( next value for MainSequence, 'admin@example.com', 1, 0, NULL, NULL, NULL, NULL, NULL, '' )
go

create table UserRequests(
	UserId int not null constraint UserRequestsUserIdFk references Users,
	RequestTime datetime2 not null
)
go
```

## Configuration

Add role constants and caching to `Development.xml`:

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

Run `sync` to generate data-access classes.

## Modification logic

Rename `UsersModification.ewlt.cs` to `UsersModification.cs` and add
pre-delete and constraint logic:

```csharp
partial class UsersModification {
	static partial void preDelete( List<UsersTableCondition> conditions, PostDeleteExecutor postDeleteExecutor ) {
		foreach( var i in UsersTableRetrieval.GetRows( conditions.ToArray() ) )
			UserRequestsModification.DeleteRows( new UserRequestsTableEqualityConditions.UserId( i.UserId ) );
	}

	static partial void populateConstraintNamesToViolationErrorMessages(
			Dictionary<string, string> constraintNamesToViolationErrorMessages ) {
		constraintNamesToViolationErrorMessages.Add(
			"UsersEmailAddressUnique", "A user with this email address already exists." );
	}
}
```

## Provider implementation

Create `Library/Providers/UserManagement.cs` implementing
`SystemUserManagementProvider`. Key methods:

- `GetIdentityProviders()` -- return `LocalIdentityProvider` (and/or SAML)
- `GetUsers()` -- return all users ordered by email
- `GetUser( int userId )` -- look up by ID
- `GetUser( string emailAddress )` -- look up by email
- `InsertOrUpdateUser()` -- create or update users
- `DeleteUser()` -- delete a user
- `GetRoles()` -- return available roles

The `LocalIdentityProvider` requires callbacks for looking up users by email,
loading/storing login codes, and updating passwords.

## Roles and access control

Create `Role` objects from your role data:

```csharp
private Role getRoleObject( int roleId ) =>
	new( roleId, UserRolesRows.GetNameFromValue( roleId ), roleId == UserRolesRows.Administrator, false );
```

The third parameter marks admin roles (grants access to the `/ewl` admin area).

## Using in pages

Check the current user in `userCanAccess`:

```csharp
// Require login
protected override bool userCanAccess => SystemUser.Current is not null;

// Require specific roles
protected override bool userCanAccess =>
	new[] { UserRolesRows.Administrator, UserRolesRows.BicycleMechanic }
		.Contains( SystemUser.Current!.Role.RoleId );
```

Access control is hierarchical: child pages inherit parent restrictions.

## Built-in admin UI

The framework provides a user management UI at `/ewl` that allows
administrators to view, create, update, and delete users. This is
automatically available when the provider is implemented.

In development mode, a "Select User" page replaces the login page so you
don't need to enter passwords.
