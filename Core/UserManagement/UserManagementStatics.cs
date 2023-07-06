using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.UserManagement.IdentityProviders;

namespace EnterpriseWebLibrary.UserManagement;

/// <summary>
/// Provides useful constants and methods pertaining to user management.
/// </summary>
public static class UserManagementStatics {
	private const string providerName = "UserManagement";
	internal const string CertificatePassword = "password";

	private static SystemProviderReference<SystemUserManagementProvider> provider;
	private static Action certificateUpdateNotifier;

	private static IReadOnlyCollection<IdentityProvider> identityProviders;
	private static LocalIdentityProvider localIdentityProvider;
	private static ( Func<string> getter, Action<string> updater )? certificateMethods;

	internal static void Init( Action certificateUpdateNotifier, Func<SystemUser> currentUserGetter ) {
		if( ConfigurationStatics.IsClientSideApp )
			return;

		SystemUser.Init( currentUserGetter );

		provider = ConfigurationStatics.GetSystemLibraryProvider<SystemUserManagementProvider>( providerName );
		UserManagementStatics.certificateUpdateNotifier = certificateUpdateNotifier;
	}

	/// <summary>
	/// EWL use only.
	/// </summary>
	public static bool UserManagementEnabled => provider.GetProvider( returnNullIfNotFound: true ) != null;

	/// <summary>
	/// EWL use only.
	/// </summary>
	public static SystemUserManagementProvider SystemProvider => provider.GetProvider();

	internal static void InitSystemSpecificLogicDependencies() {
		if( ConfigurationStatics.IsClientSideApp || !UserManagementEnabled )
			return;
		identityProviders = SystemProvider.GetIdentityProviders().Materialize();
		localIdentityProvider = identityProviders.OfType<LocalIdentityProvider>().SingleOrDefault();
		certificateMethods = SystemProvider.GetCertificateMethods();
	}

	internal static IReadOnlyCollection<IdentityProvider> IdentityProviders => identityProviders;

	internal static bool LocalIdentityProviderEnabled => localIdentityProvider != null;

	internal static LocalIdentityProvider LocalIdentityProvider => localIdentityProvider;

	internal static string GetCertificate() =>
		certificateMethods.HasValue ? certificateMethods.Value.getter() : throw new ApplicationException( "Self-signed certificate methods not available." );

	internal static void UpdateCertificate( string certificate ) {
		if( !certificateMethods.HasValue )
			throw new ApplicationException( "Self-signed certificate methods not available." );
		certificateMethods.Value.updater( certificate );
		certificateUpdateNotifier();
	}

	internal static SystemUser GetUser( int userId, bool ensureUserExists ) {
		var user = SystemProvider.GetUser( userId );
		if( user == null && ensureUserExists )
			throw new ApplicationException( "A user with an ID of {0} does not exist.".FormatWith( userId ) );
		return user;
	}
}