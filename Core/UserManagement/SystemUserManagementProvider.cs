using NodaTime;

namespace EnterpriseWebLibrary.UserManagement;

/// <summary>
/// Defines how user management operations will be carried out against the database for a particular application.
/// </summary>
public abstract class SystemUserManagementProvider {
	/// <summary>
	/// Returns the identity providers for the system.
	/// </summary>
	protected internal abstract IEnumerable<IdentityProvider> GetIdentityProviders();

	/// <summary>
	/// Returns a pair of methods for managing the installation’s self-signed certificate, or null if a certificate is not supported.
	/// </summary>
	protected internal virtual ( Func<string> getter, Action<string> updater )? GetCertificateMethods() => null;

	/// <summary>
	/// Retrieves all users.
	/// </summary>
	protected internal abstract IEnumerable<SystemUser> GetUsers();

	/// <summary>
	/// Returns the user with the specified ID, or null if a user with that ID does not exist.
	/// </summary>
	protected internal abstract SystemUser GetUser( int userId );

	/// <summary>
	/// Returns the user with the specified email address, or null if a user with that email address does not exist. Do not pass null. We recommend that you use
	/// case-insensitive comparison. This method exists to support passwordless authentication and user impersonation.
	/// </summary>
	protected internal abstract SystemUser GetUser( string emailAddress );

	/// <summary>
	/// Inserts a new user (if no user ID is passed) or updates an existing user with the specified parameters. Returns the user’s ID.
	/// </summary>
	protected internal abstract int InsertOrUpdateUser( int? userId, string emailAddress, int roleId, Instant? lastRequestTime );

	/// <summary>
	/// Deletes the user with the specified ID.
	/// </summary>
	protected internal abstract void DeleteUser( int userId );

	/// <summary>
	/// Retrieves all roles.
	/// </summary>
	protected internal abstract IEnumerable<Role> GetRoles();
}