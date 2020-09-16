# Database scripts

## SQL Server

```SQL
create table UserRoles(
	UserRoleId int
		not null
		constraint UserRolesPk primary key,
	RoleName varchar( 100 )
		not null
)
go

insert into UserRoles values( 1, 'Administrator' )
insert into UserRoles values( 2, 'Standard user' )
go

create table Users(
	UserId int
		not null
		constraint UsersPk primary key,
	EmailAddress varchar( 100 )
		not null
		constraint UsersEmailAddressUnique unique,
	RoleId int
		not null
		constraint UsersRoleIdFk references UserRoles,
	LastRequestDateAndTime datetime2
		null,
	Salt int
		not null,
	SaltedPassword varbinary( 20 )
		null,
	MustChangePassword bit
		not null
)
go

insert into MainSequence default values
insert into Users values( @@IDENTITY, 'john.doe@example.com', 1, NULL, 0, NULL, 1 );
go
```

## MySQL

```SQL
Not yet documented
```

## Oracle

```PLSQL
Not yet documented
```