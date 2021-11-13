using System;
using System.Collections.Generic;
using System.Linq;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.UserManagement.IdentityProviders;
using Humanizer;
using Tewl.Tools;

namespace EnterpriseWebLibrary.UserManagement {
	/// <summary>
	/// Provides useful constants and methods pertaining to user management.
	/// </summary>
	public static class UserManagementStatics {
		private const string providerName = "UserManagement";

		private static SystemProviderReference<SystemUserManagementProvider> provider;

		private static IReadOnlyCollection<IdentityProvider> identityProviders;
		private static LocalIdentityProvider localIdentityProvider;

		internal static void Init() {
			provider = ConfigurationStatics.GetSystemLibraryProvider<SystemUserManagementProvider>( providerName );
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
			if( !UserManagementEnabled )
				return;
			identityProviders = SystemProvider.GetIdentityProviders().Materialize();
			localIdentityProvider = identityProviders.OfType<LocalIdentityProvider>().SingleOrDefault();
		}

		internal static IReadOnlyCollection<IdentityProvider> IdentityProviders => identityProviders;

		internal static bool LocalIdentityProviderEnabled => localIdentityProvider != null;

		internal static LocalIdentityProvider LocalIdentityProvider => localIdentityProvider;

		internal static User GetUser( int userId, bool ensureUserExists ) {
			var user = SystemProvider.GetUser( userId );
			if( user == null && ensureUserExists )
				throw new ApplicationException( "A user with an ID of {0} does not exist.".FormatWith( userId ) );
			return user;
		}
	}
}