using EnterpriseWebLibrary.UserManagement;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;

/// <summary>
/// A configuration for automatic user registration via the local identity provider on the default log-in page for the application.
/// </summary>
/// <param name="AllowedEmailAddressDomains">The collection of allowed domains. If the collection is not empty, an email address must be within one of these
/// domains to be automatically registered.</param>
/// <param name="NewUserRoleId">The authorization role ID that newly-registered users will have.</param>
public sealed record LogInPageAutoUserRegistrationSetup( IReadOnlyCollection<string> AllowedEmailAddressDomains, int NewUserRoleId ) {
	public int? GetRoleIdForEmailAddress( string email ) =>
		!AllowedEmailAddressDomains.Any() ? NewUserRoleId : SystemUser.EmailAddressWithinDomain( email, AllowedEmailAddressDomains ) ? NewUserRoleId : null;
}