using System.Collections.Generic;
using EnterpriseWebLibrary.UserManagement;
using EnterpriseWebLibrary.UserManagement.IdentityProviders;
using NodaTime;
using Tewl.Tools;

namespace EnterpriseWebLibrary.Configuration.Providers {
	internal class UserManagement: SystemUserManagementProvider {
		IEnumerable<IdentityProvider> SystemUserManagementProvider.GetIdentityProviders() =>
			new LocalIdentityProvider(
				"",
				"",
				emailAddress => ( createUser(), 1, null, false ),
				( emailAddress, password ) => ( "You should never see this", "You should never see this.".GetTextAsEncodedHtml() ),
				( userId, salt, saltedPassword, mustChangePassword ) => {} ).ToCollection();

		IEnumerable<User> SystemUserManagementProvider.GetUsers() => createUser().ToCollection();

		User SystemUserManagementProvider.GetUser( int userId ) => createUser();

		User SystemUserManagementProvider.GetUser( string emailAddress ) => createUser();

		private User createUser() => new User( 1, "john.doe@example.com", createRole(), null );

		int SystemUserManagementProvider.InsertOrUpdateUser( int? userId, string emailAddress, int roleId, Instant? lastRequestTime ) => 1;

		void SystemUserManagementProvider.DeleteUser( int userId ) {}

		IEnumerable<Role> SystemUserManagementProvider.GetRoles() => createRole().ToCollection();

		private Role createRole() => new Role( 1, "Admin", true, false );
	}
}