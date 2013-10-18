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
		List<FormsAuthCapableUser> GetUsers();

		/// <summary>
		/// Retrieves the user with the specified ID.
		/// </summary>
		FormsAuthCapableUser GetUser( int userId );

		/// <summary>
		/// Retrieves the user with the specified email address.
		/// </summary>
		FormsAuthCapableUser GetUser( string email );

		/// <summary>
		/// Inserts a new user (if no user ID is passed) or updates an existing user with the specified parameters.
		/// </summary>
		void InsertOrUpdateUser( int? userId, string email, int salt, byte[] saltedPassword, int roleId, DateTime? lastRequestDateTime, bool mustChangePassword );

		/// <summary>
		/// Gets the subject and body of the message that will be sent to the specified user when a password reset is requested.
		/// <paramref name="bodyHtml"/> must be HTML, so if needed call GetTextAsEncodedHtml() on plain text.
		/// </summary>
		void GetPasswordResetParams( string email, string password, out string subject, out string bodyHtml );

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