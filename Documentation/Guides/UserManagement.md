# Adding user management

Last updated for Enterprise Web Library version 65.


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
using System.IO;
using System.Linq;
using EnterpriseWebLibrary;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;
using Humanizer;
using NodaTime;
using ServiceManager.Library.DataAccess;
using ServiceManager.Library.DataAccess.CommandConditions;
using ServiceManager.Library.DataAccess.Modification;
using ServiceManager.Library.DataAccess.RowConstants;
using ServiceManager.Library.DataAccess.TableRetrieval;

namespace ServiceManager.Library.Configuration.Providers {
	internal class UserManagement: FormsAuthCapableUserManagementProvider {
		void SystemUserManagementProvider.DeleteUser( int userId ) => UsersModification.DeleteRows( new UsersTableEqualityConditions.UserId( userId ) );

		IEnumerable<Role> SystemUserManagementProvider.GetRoles() => UserRolesTableRetrieval.GetAllRows().Select( i => getRoleObject( i.UserRoleId ) );

		IEnumerable<FormsAuthCapableUser> FormsAuthCapableUserManagementProvider.GetUsers() =>
			UsersTableRetrieval.GetRows().OrderBy( i => i.EmailAddress ).Select( getUserObject );

		FormsAuthCapableUser FormsAuthCapableUserManagementProvider.GetUser( int userId ) =>
			getUserObject( UsersTableRetrieval.GetRowMatchingId( userId, returnNullIfNoMatch: true ) );

		FormsAuthCapableUser FormsAuthCapableUserManagementProvider.GetUser( string email ) =>
			getUserObject( UsersTableRetrieval.GetRows( new UsersTableEqualityConditions.EmailAddress( email ) ).SingleOrDefault() );

		private FormsAuthCapableUser getUserObject( UsersTableRetrieval.Row user ) =>
			user == null
				? null
				: new FormsAuthCapableUser(
					user.UserId,
					user.EmailAddress,
					getRoleObject( user.RoleId ),
					user.LastRequestDateAndTime.ToNewUnderlyingValue( v => LocalDateTime.FromDateTime( v ).InUtc().ToInstant() ),
					user.Salt,
					user.SaltedPassword,
					user.MustChangePassword );

		private Role getRoleObject( int roleId ) => new Role( roleId, UserRolesRows.GetNameFromValue( roleId ), roleId == UserRolesRows.Administrator, false );

		void FormsAuthCapableUserManagementProvider.InsertOrUpdateUser(
			int? userId, string email, int roleId, Instant? lastRequestTime, int salt, byte[] saltedPassword, bool mustChangePassword ) {
			if( userId.HasValue ) {
				var mod = UsersModification.CreateForUpdate( new UsersTableEqualityConditions.UserId( userId.Value ) );
				mod.EmailAddress = email;
				mod.RoleId = roleId;
				mod.LastRequestDateAndTime = lastRequestTime?.InUtc().ToDateTimeUnspecified();
				mod.Salt = salt;
				mod.SaltedPassword = saltedPassword;
				mod.MustChangePassword = mustChangePassword;
				mod.Execute();
			}
			else
				UsersModification.InsertRow(
					MainSequence.GetNextValue(),
					email,
					roleId,
					lastRequestTime?.InUtc().ToDateTimeUnspecified(),
					salt,
					saltedPassword,
					mustChangePassword );
		}

		void FormsAuthCapableUserManagementProvider.GetPasswordResetParams( string email, string password, out string subject, out string bodyHtml ) {
			subject = "{0} - New password".FormatWith( ConfigurationStatics.SystemName );
			using( var sw = new StringWriter() ) {
				sw.WriteLine( "Thank you for using {0}. Your new temporary password is:".FormatWith( ConfigurationStatics.SystemName ) );
				sw.WriteLine();
				sw.WriteLine( password );
				sw.WriteLine();
				sw.WriteLine(
					"You can use this password to log in at {0}.".FormatWith(
						ConfigurationStatics.GetWebApplicationDefaultBaseUrl( WebApplicationNames.Website, false ) ) );
				sw.WriteLine();
				sw.WriteLine( "Thanks again," );
				sw.WriteLine( "Organization Name" );
				bodyHtml = sw.ToString().GetTextAsEncodedHtml();
			}
		}

		string FormsAuthCapableUserManagementProvider.AdministratingCompanyName => "Organization Name";
		string FormsAuthCapableUserManagementProvider.LogInHelpInstructions => "contact Organization Name.";
	}
}
```

Note that you can implement `StrictFormsAuthUserManagementProvider` instead if you’d like.