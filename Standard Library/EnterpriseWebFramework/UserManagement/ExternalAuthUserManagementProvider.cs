using System;
using System.Collections.Generic;
using RedStapler.StandardLibrary.DataAccess;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement {
	/// <summary>
	/// Defines how external authentication user management operations will be carried out against the database for a particular application.
	/// </summary>
	public interface ExternalAuthUserManagementProvider: SystemUserManagementProvider {
		/// <summary>
		/// Retrieves all users.
		/// </summary>
		List<ExternalAuthUser> GetUsers( DBConnection cn );

		/// <summary>
		/// Retrieves the user with the specified ID.
		/// </summary>
		ExternalAuthUser GetUser( DBConnection cn, int userId );

		/// <summary>
		/// Retrieves the user with the specified email address.
		/// </summary>
		ExternalAuthUser GetUser( DBConnection cn, string email );

		/// <summary>
		/// Inserts a new user (if no user ID is passed) or updates an existing user with the specified parameters.
		/// </summary>
		void InsertOrUpdateUser( DBConnection cn, int? userId, string email, int roleId, DateTime? lastRequestDateTime );
	}
}