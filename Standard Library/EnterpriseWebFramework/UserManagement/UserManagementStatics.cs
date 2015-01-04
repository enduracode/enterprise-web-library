using System;
using System.Collections.Generic;
using System.Web;
using RedStapler.StandardLibrary.Configuration;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement {
	/// <summary>
	/// Provides useful constants and methods pertaining to user management.
	/// </summary>
	public static class UserManagementStatics {
		private const string providerName = "UserManagement";

		/// <summary>
		/// Do not use directly. Use <see cref="SystemProvider"/>.
		/// </summary>
		private static SystemUserManagementProvider provider;

		internal static void Init() {
			provider = ConfigurationStatics.GetSystemLibraryProvider( providerName ) as SystemUserManagementProvider;
			FormsAuthStatics.Init( () => SystemProvider );
		}

		/// <summary>
		/// Standard Library use only.
		/// </summary>
		public static bool UserManagementEnabled { get { return provider != null; } }

		/// <summary>
		/// Standard Library use only.
		/// </summary>
		public static SystemUserManagementProvider SystemProvider {
			get {
				if( provider == null )
					throw ConfigurationStatics.CreateProviderNotFoundException( providerName );
				return provider;
			}
		}

		// NOTE: It seems like we could cache a collection of Roles and have users just take a roleId, and look up the object ourselves. This would save the apps
		// creating the role object, and all save the extra database query.  But where would we do this?
		/// <summary>
		/// Standard Library use only.
		/// </summary>
		public static IEnumerable<User> GetUsers() {
			if( SystemProvider is FormsAuthCapableUserManagementProvider )
				return FormsAuthStatics.GetUsers();
			if( SystemProvider is ExternalAuthUserManagementProvider )
				return ( SystemProvider as ExternalAuthUserManagementProvider ).GetUsers();
			throw new ApplicationException( "Unknown user management setup type." );
		}

		/// <summary>
		/// Standard Library use only.
		/// </summary>
		public static User GetUser( int userId, bool ensureUserExists ) {
			if( SystemProvider is FormsAuthCapableUserManagementProvider )
				return FormsAuthStatics.GetUser( userId, ensureUserExists );
			if( SystemProvider is ExternalAuthUserManagementProvider ) {
				var user = ( SystemProvider as ExternalAuthUserManagementProvider ).GetUser( userId );
				if( user == null && ensureUserExists )
					throw new ApplicationException( "A user with an ID of {0} does not exist.".FormatWith( userId ) );
				return user;
			}
			throw new ApplicationException( "Unknown user management setup type." );
		}

		/// <summary>
		/// Standard Library use only.
		/// </summary>
		public static User GetUser( string emailAddress ) {
			if( SystemProvider is FormsAuthCapableUserManagementProvider )
				return ( SystemProvider as FormsAuthCapableUserManagementProvider ).GetUser( emailAddress );
			if( SystemProvider is ExternalAuthUserManagementProvider )
				return ( SystemProvider as ExternalAuthUserManagementProvider ).GetUser( emailAddress );
			throw new ApplicationException( "Unknown user management setup type." );
		}

		internal static User GetUserFromRequest() {
			var cookie = HttpContext.Current.Request.Cookies[ FormsAuthStatics.FormsAuthCookieName ];
			if( cookie != null ) {
				var ticket = FormsAuthStatics.GetFormsAuthTicket( cookie );
				if( ticket != null )
					return GetUser( int.Parse( ticket.Name ), false );
			}

			var identity = HttpContext.Current.User.Identity;
			if( identity.IsAuthenticated && identity.AuthenticationType == CertificateAuthenticationModule.CertificateAuthenticationType )
				return GetUser( identity.Name );

			return null;
		}
	}
}