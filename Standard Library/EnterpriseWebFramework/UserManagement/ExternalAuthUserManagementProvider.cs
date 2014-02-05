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
		IEnumerable<ExternalAuthUser> GetUsers();

		/// <summary>
		/// Returns the user with the specified ID, or null if a user with that ID does not exist.
		/// </summary>
		ExternalAuthUser GetUser( int userId );

		/// <summary>
		/// Returns the user with the specified email address, or null if a user with that email address does not exist.
		/// </summary>
		ExternalAuthUser GetUser( string email );

		/// <summary>
		/// Inserts a new user (if no user ID is passed) or updates an existing user with the specified parameters.
		/// </summary>
		void InsertOrUpdateUser( int? userId, string email, int roleId, DateTime? lastRequestDateTime );
	}
}