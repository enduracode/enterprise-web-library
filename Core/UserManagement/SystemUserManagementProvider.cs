using System.Collections.Generic;
using NodaTime;

namespace EnterpriseWebLibrary.UserManagement {
	/// <summary>
	/// Defines how user management operations will be carried out against the database for a particular application.
	/// </summary>
	public interface SystemUserManagementProvider {
		/// <summary>
		/// Returns the identity providers for the system.
		/// </summary>
		IEnumerable<IdentityProvider> GetIdentityProviders();

		/// <summary>
		/// Retrieves all users.
		/// </summary>
		IEnumerable<User> GetUsers();

		/// <summary>
		/// Returns the user with the specified ID, or null if a user with that ID does not exist.
		/// </summary>
		User GetUser( int userId );

		/// <summary>
		/// Returns the user with the specified email address, or null if a user with that email address does not exist. Do not pass null. We recommend that you use
		/// case-insensitive comparison. This method exists to support user impersonation.
		/// </summary>
		User GetUser( string emailAddress );

		/// <summary>
		/// Inserts a new user (if no user ID is passed) or updates an existing user with the specified parameters. Returns the user’s ID.
		/// </summary>
		int InsertOrUpdateUser( int? userId, string emailAddress, int roleId, Instant? lastRequestTime );

		/// <summary>
		/// Deletes the user with the specified ID.
		/// </summary>
		void DeleteUser( int userId );

		/// <summary>
		/// Retrieves all roles.
		/// </summary>
		IEnumerable<Role> GetRoles();
	}
}