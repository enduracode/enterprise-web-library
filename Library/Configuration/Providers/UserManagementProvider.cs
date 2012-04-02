using System;
using System.Collections.Generic;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement;

namespace RedStapler.StandardLibrary.Configuration.Providers {
	internal class UserManagementProvider: FormsAuthCapableUserManagementProvider {
		void SystemUserManagementProvider.DeleteUser( DBConnection cn, int userId ) {}

		List<Role> SystemUserManagementProvider.GetRoles( DBConnection cn ) {
			return new List<Role> { createRole() };
		}

		List<FormsAuthCapableUser> FormsAuthCapableUserManagementProvider.GetUsers( DBConnection cn ) {
			return new List<FormsAuthCapableUser> { createUser() };
		}

		FormsAuthCapableUser FormsAuthCapableUserManagementProvider.GetUser( DBConnection cn, int userId ) {
			return createUser();
		}

		FormsAuthCapableUser FormsAuthCapableUserManagementProvider.GetUser( DBConnection cn, string email ) {
			return createUser();
		}

		private static FormsAuthCapableUser createUser() {
			return new FormsAuthCapableUser( 1, "fake@redstapler.biz", createRole(), null, 1, "test", false, "" );
		}

		private static Role createRole() {
			return new Role( 1, "Admin", true, false );
		}

		void FormsAuthCapableUserManagementProvider.InsertOrUpdateUser( DBConnection cn, int? userId, string email, int salt, string saltedPassword, int roleId,
		                                                                DateTime? lastRequestDateTime, bool mustChangePassword ) {}

		void FormsAuthCapableUserManagementProvider.GetPasswordResetParams( string email, string password, out string subject, out string body ) {
			subject = "You should never see this";
			body = "You should never see this.";
		}

		string FormsAuthCapableUserManagementProvider.AdministratingCompanyName { get { return ""; } }

		string FormsAuthCapableUserManagementProvider.LogInHelpInstructions { get { return ""; } }
	}
}