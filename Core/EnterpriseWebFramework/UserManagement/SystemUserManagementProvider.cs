using System.Collections.Generic;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement {
	/// <summary>
	/// Defines how user management operations will be carried out against the database for a particular application.
	/// </summary>
	public interface SystemUserManagementProvider {
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