using System;
using System.Collections.Generic;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement {
	/// <summary>
	/// Defines how external authentication user management operations will be carried out against the database for a particular application.
	/// </summary>
	public interface ExternalAuthUserManagementProvider: SystemUserManagementProvider {
		/// <summary>
		/// Retrieves all users.
		/// </summary>
		List<ExternalAuthUser> GetUsers();

		/// <summary>
		/// Retrieves the user with the specified ID.
		/// </summary>
		ExternalAuthUser GetUser( int userId );

		/// <summary>
		/// Retrieves the user with the specified email address.
		/// </summary>
		ExternalAuthUser GetUser( string email );

		/// <summary>
		/// Inserts a new user (if no user ID is passed) or updates an existing user with the specified parameters.
		/// </summary>
		void InsertOrUpdateUser( int? userId, string email, int roleId, DateTime? lastRequestDateTime );
	}
}