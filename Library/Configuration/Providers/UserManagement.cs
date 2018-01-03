using System.Collections.Generic;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;
using NodaTime;

namespace EnterpriseWebLibrary.Configuration.Providers {
	internal class UserManagement: FormsAuthCapableUserManagementProvider {
		void SystemUserManagementProvider.DeleteUser( int userId ) {}

		IEnumerable<Role> SystemUserManagementProvider.GetRoles() {
			return new List<Role> { createRole() };
		}

		private Role createRole() {
			return new Role( 1, "Admin", true, false );
		}

		IEnumerable<FormsAuthCapableUser> FormsAuthCapableUserManagementProvider.GetUsers() {
			return new List<FormsAuthCapableUser> { createUser() };
		}

		FormsAuthCapableUser FormsAuthCapableUserManagementProvider.GetUser( int userId ) {
			return createUser();
		}

		FormsAuthCapableUser FormsAuthCapableUserManagementProvider.GetUser( string email ) {
			return createUser();
		}

		private FormsAuthCapableUser createUser() {
			return new FormsAuthCapableUser( 1, "john.doe@example.com", createRole(), null, 1, null, false );
		}

		void FormsAuthCapableUserManagementProvider.InsertOrUpdateUser(
			int? userId, string email, int roleId, Instant? lastRequestTime, int salt, byte[] saltedPassword, bool mustChangePassword ) {}

		void FormsAuthCapableUserManagementProvider.GetPasswordResetParams( string email, string password, out string subject, out string bodyHtml ) {
			subject = "You should never see this";
			bodyHtml = "You should never see this.".GetTextAsEncodedHtml();
		}

		string FormsAuthCapableUserManagementProvider.AdministratingCompanyName => "";
		string FormsAuthCapableUserManagementProvider.LogInHelpInstructions => "";
	}
}