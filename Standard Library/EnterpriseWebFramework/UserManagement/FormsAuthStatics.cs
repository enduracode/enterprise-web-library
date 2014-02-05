using System;
using System.Collections.Generic;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement {
	/// <summary>
	/// Statics related to forms authentication.
	/// </summary>
	public static class FormsAuthStatics {
		private static Func<SystemUserManagementProvider> providerGetter;

		internal static void Init( Func<SystemUserManagementProvider> providerGetter ) {
			FormsAuthStatics.providerGetter = providerGetter;
		}

		/// <summary>
		/// Standard Library use only.
		/// </summary>
		public static bool FormsAuthEnabled { get { return providerGetter() is FormsAuthCapableUserManagementProvider; } }

		/// <summary>
		/// Standard Library use only.
		/// </summary>
		public static FormsAuthCapableUserManagementProvider SystemProvider { get { return (FormsAuthCapableUserManagementProvider)providerGetter(); } }

		internal static IEnumerable<FormsAuthCapableUser> GetUsers() {
			return SystemProvider.GetUsers();
		}

		internal static FormsAuthCapableUser GetUser( int userId, bool ensureUserExists ) {
			var user = SystemProvider.GetUser( userId );
			if( user == null && ensureUserExists )
				throw new ApplicationException( "A user with an ID of {0} does not exist.".FormatWith( userId ) );
			return user;
		}

		internal static FormsAuthCapableUser GetUser( string emailAddress ) {
			return SystemProvider.GetUser( emailAddress );
		}
	}
}