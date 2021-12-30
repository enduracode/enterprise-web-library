# Adding user management

Last updated for Enterprise Web Library version 72.


## Creating database schema

First we need to add schema and reference data to your database to support users and roles. Use one of the following scripts as a starting point:

*	[SQL Server](UserManagementSupplements/DatabaseScripts.md#sql-server)
*	[MySQL](UserManagementSupplements/DatabaseScripts.md#mysql)
*	[Oracle](UserManagementSupplements/DatabaseScripts.md#oracle)

Remember to use a real email address for the first user. Feel free to make other modifications to the script, in particular changing the roles or adding columns to the `Users` table. The only constraint is that you must implement the provider interface in the next section.

Now open `Library/Configuration/Development.xml` and add entries for your roles table to the `<database>` element:

```XML
<database>
	<rowConstantTables>
		<table tableName="UserRoles" nameColumn="RoleName" valueColumn="UserRoleId" />
	</rowConstantTables>
	<SmallTables>
		<Table>UserRoles</Table>
	</SmallTables>
</database>
```

Run `Update-DependentLogic`.


## Implementing the provider

Add a class called `UserManagement` to your `Library/Configuration/Providers` folder. Paste the following into it, making adjustments as necessary to match the schema you created above:

```C#
using System.Collections.Generic;
using System.Linq;
using EnterpriseWebLibrary;
using EnterpriseWebLibrary.UserManagement;
using EnterpriseWebLibrary.UserManagement.IdentityProviders;
using NodaTime;
using ServiceManager.Library.DataAccess;
using ServiceManager.Library.DataAccess.CommandConditions;
using ServiceManager.Library.DataAccess.Modification;
using ServiceManager.Library.DataAccess.RowConstants;
using ServiceManager.Library.DataAccess.TableRetrieval;
using Tewl.Tools;

namespace ServiceManager.Library.Configuration.Providers {
	internal class UserManagement: SystemUserManagementProvider {
		protected override IEnumerable<IdentityProvider> GetIdentityProviders() =>
			new LocalIdentityProvider(
				"Organization Name",
				"contact Organization Name.",
				emailAddress => {
					var user = UsersTableRetrieval.GetRows( new UsersTableEqualityConditions.EmailAddress( emailAddress ) ).SingleOrDefault();
					if( user == null )
						return null;
					return ( getUserObject( user ), user.Salt, user.SaltedPassword );
				},
				userId => {
					var user = UsersTableRetrieval.GetRowMatchingId( userId );
					return ( user.LoginCodeSalt, user.HashedLoginCode,
						       user.LoginCodeExpirationDateAndTime.ToNewUnderlyingValue( v => LocalDateTime.FromDateTime( v ).InUtc().ToInstant() ),
						       user.LoginCodeRemainingAttemptCount, user.LoginCodeDestinationUrl );
				},
				( userId, salt, saltedPassword ) => {
					var mod = UsersModification.CreateForUpdate( new UsersTableEqualityConditions.UserId( userId ) );
					mod.Salt = salt;
					mod.SaltedPassword = saltedPassword;
					mod.Execute();
				},
				( userId, salt, hashedCode, expirationTime, remainingAttemptCount, destinationUrl ) => {
					var mod = UsersModification.CreateForUpdate( new UsersTableEqualityConditions.UserId( userId ) );
					mod.LoginCodeSalt = salt;
					mod.HashedLoginCode = hashedCode;
					mod.LoginCodeExpirationDateAndTime = expirationTime?.InUtc().ToDateTimeUnspecified();
					mod.LoginCodeRemainingAttemptCount = remainingAttemptCount;
					mod.LoginCodeDestinationUrl = destinationUrl;
					mod.Execute();
				} ).ToCollection();

		protected override IEnumerable<User> GetUsers() => UsersTableRetrieval.GetRows().OrderBy( i => i.EmailAddress ).Select( getUserObject );

		protected override User GetUser( int userId ) => getUserObject( UsersTableRetrieval.GetRowMatchingId( userId, returnNullIfNoMatch: true ) );

		protected override User GetUser( string emailAddress ) =>
			getUserObject( UsersTableRetrieval.GetRows( new UsersTableEqualityConditions.EmailAddress( emailAddress ) ).SingleOrDefault() );

		private User getUserObject( UsersTableRetrieval.Row user ) =>
			user == null
				? null
				: new User(
					user.UserId,
					user.EmailAddress,
					getRoleObject( user.RoleId ),
					user.LastRequestDateAndTime.ToNewUnderlyingValue( v => LocalDateTime.FromDateTime( v ).InUtc().ToInstant() ) );

		private Role getRoleObject( int roleId ) => new Role( roleId, UserRolesRows.GetNameFromValue( roleId ), roleId == UserRolesRows.Administrator, false );

		protected override int InsertOrUpdateUser( int? userId, string emailAddress, int roleId, Instant? lastRequestTime ) {
			if( userId.HasValue ) {
				var mod = UsersModification.CreateForUpdate( new UsersTableEqualityConditions.UserId( userId.Value ) );
				mod.EmailAddress = emailAddress;
				mod.RoleId = roleId;
				mod.LastRequestDateAndTime = lastRequestTime?.InUtc().ToDateTimeUnspecified();
				mod.Execute();
			}
			else {
				userId = MainSequence.GetNextValue();
				UsersModification.InsertRow(
					userId.Value,
					emailAddress,
					roleId,
					lastRequestTime?.InUtc().ToDateTimeUnspecified(),
					0,
					null,
					null,
					null,
					null,
					null,
					"" );
			}
			return userId.Value;
		}

		protected override void DeleteUser( int userId ) => UsersModification.DeleteRows( new UsersTableEqualityConditions.UserId( userId ) );

		protected override IEnumerable<Role> GetRoles() => UserRolesTableRetrieval.GetAllRows().Select( i => getRoleObject( i.UserRoleId ) );
	}
}
```