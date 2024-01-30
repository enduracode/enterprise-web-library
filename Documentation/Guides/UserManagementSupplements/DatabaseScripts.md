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
	LoginCodeSalt varbinary( 16 )
		null,
	HashedLoginCode varbinary( 20 )
		null,
	LoginCodeExpirationDateAndTime datetime2
		null,
	LoginCodeRemainingAttemptCount tinyint
		null,
	LoginCodeDestinationUrl varchar( 500 )
		not null
)
go

insert into Users values( next value for MainSequence, 'john.doe@example.com', 1, NULL, 0, NULL, NULL, NULL, NULL, NULL, '' );
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