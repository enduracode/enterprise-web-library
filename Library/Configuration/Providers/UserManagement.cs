﻿using System.Collections.Generic;
using EnterpriseWebLibrary.UserManagement;
using EnterpriseWebLibrary.UserManagement.IdentityProviders;
using NodaTime;
using Tewl.Tools;

namespace EnterpriseWebLibrary.Configuration.Providers {
	internal class UserManagement: SystemUserManagementProvider {
		protected override IEnumerable<IdentityProvider> GetIdentityProviders() =>
			new LocalIdentityProvider(
				"",
				"",
				emailAddress => ( createUser(), 1, null, false ),
				( emailAddress, password ) => ( "You should never see this", "You should never see this.".GetTextAsEncodedHtml() ),
				( userId, salt, saltedPassword, mustChangePassword ) => {} ).ToCollection();

		protected override IEnumerable<User> GetUsers() => createUser().ToCollection();

		protected override User GetUser( int userId ) => createUser();

		protected override User GetUser( string emailAddress ) => createUser();

		private User createUser() => new User( 1, "john.doe@example.com", createRole(), null );

		protected override int InsertOrUpdateUser( int? userId, string emailAddress, int roleId, Instant? lastRequestTime ) => 1;

		protected override void DeleteUser( int userId ) {}

		protected override IEnumerable<Role> GetRoles() => createRole().ToCollection();

		private Role createRole() => new Role( 1, "Admin", true, false );
	}
}