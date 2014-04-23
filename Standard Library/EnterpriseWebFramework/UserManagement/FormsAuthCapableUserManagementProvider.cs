using System;
using System.Collections.Generic;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement {
	/// <summary>
	/// Defines how user management operations will be carried out against the database for a particular application. Supports ASP.NET Forms Authentication.
	/// </summary>
	public interface FormsAuthCapableUserManagementProvider: SystemUserManagementProvider {
		/// <summary>
		/// Retrieves all users.
		/// </summary>
		IEnumerable<FormsAuthCapableUser> GetUsers();

		/// <summary>
		/// Returns the user with the specified ID, or null if a user with that ID does not exist.
		/// </summary>
		FormsAuthCapableUser GetUser( int userId );

		/// <summary>
		/// Returns the user with the specified email address, or null if a user with that email address does not exist. We recommend that you use case-insensitive
		/// comparison.
		/// </summary>
		FormsAuthCapableUser GetUser( string email );

		/// <summary>
		/// Inserts a new user (if no user ID is passed) or updates an existing user with the specified parameters.
		/// </summary>
		void InsertOrUpdateUser( int? userId, string email, int salt, byte[] saltedPassword, int roleId, DateTime? lastRequestDateTime, bool mustChangePassword );

		/// <summary>
		/// Gets the subject and body of the message that will be sent to the specified user when a password reset is requested.
		/// Body is HTML encoded automatically.
		/// </summary>
		void GetPasswordResetParams( string email, string password, out string subject, out string body );

		/// <summary>
		/// Gets the name of the company responsible for administrating the web site.
		/// </summary>
		string AdministratingCompanyName { get; }

		/// <summary>
		/// Gets the text explaining what to do if the user has trouble logging in.  An example is "call 555-555-5555." or "talk to XXX."
		/// </summary>
		string LogInHelpInstructions { get; }
	}
}