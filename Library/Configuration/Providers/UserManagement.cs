using EnterpriseWebLibrary.UserManagement;
using EnterpriseWebLibrary.UserManagement.IdentityProviders;
using JetBrains.Annotations;
using NodaTime;

namespace EnterpriseWebLibrary.Configuration.Providers;

[ UsedImplicitly ]
internal class UserManagement: SystemUserManagementProvider {
	protected override IEnumerable<IdentityProvider> GetIdentityProviders() =>
		new LocalIdentityProvider(
			"{0} Team".FormatWith( EwlStatics.EwlInitialism ),
			"contact the {0} Team.".FormatWith( EwlStatics.EwlInitialism ),
			emailAddress => ( createUser(), 1, null ),
			userId => ( null, null, null, null, "" ),
			( userId, salt, saltedPassword ) => {},
			( userId, salt, hashedCode, expirationTime, remainingAttemptCount, destinationUrl ) => {} ).ToCollection();

	protected override IEnumerable<SystemUser> GetUsers() => createUser().ToCollection();

	protected override SystemUser GetUser( int userId ) => createUser();

	protected override SystemUser GetUser( string emailAddress ) => createUser();

	private SystemUser createUser() => new( 1, "john.doe@example.com", createRole(), null );

	protected override int InsertOrUpdateUser( int? userId, string emailAddress, int roleId, Instant? lastRequestTime ) => 1;

	protected override void DeleteUser( int userId ) {}

	protected override IEnumerable<Role> GetRoles() => createRole().ToCollection();

	private Role createRole() => new( 1, "Admin", true, false );
}