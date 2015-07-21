using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Humanizer;
using EnterpriseWebLibrary.Configuration;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement {
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
		/// EWL use only.
		/// </summary>
		public static bool UserManagementEnabled { get { return provider != null; } }

		/// <summary>
		/// EWL use only.
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
		/// EWL use only.
		/// </summary>
		public static IEnumerable<User> GetUsers() {
			if( SystemProvider is FormsAuthCapableUserManagementProvider )
				return FormsAuthStatics.GetUsers();
			if( SystemProvider is ExternalAuthUserManagementProvider )
				return ( SystemProvider as ExternalAuthUserManagementProvider ).GetUsers();
			throw new ApplicationException( "Unknown user management setup type." );
		}

		/// <summary>
		/// EWL use only.
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
		/// EWL use only.
		/// </summary>
		public static User GetUser( string emailAddress ) {
			if( SystemProvider is FormsAuthCapableUserManagementProvider )
				return ( SystemProvider as FormsAuthCapableUserManagementProvider ).GetUser( emailAddress );
			if( SystemProvider is ExternalAuthUserManagementProvider )
				return ( SystemProvider as ExternalAuthUserManagementProvider ).GetUser( emailAddress );
			throw new ApplicationException( "Unknown user management setup type." );
		}

		/// <summary>
		/// The second item in the returned tuple will be (1) null if impersonation is not taking place, (2) a tuple with a null user if impersonation is taking
		/// place with an impersonator who doesn't correspond to a user, or (3) a tuple containing the impersonator.
		/// </summary>
		internal static Tuple<User, Tuple<User>> GetUserAndImpersonatorFromRequest() {
			var userLazy = new Func<User>[]
				{
					() => {
						var cookie = CookieStatics.GetCookie( FormsAuthStatics.FormsAuthCookieName );
						if( cookie == null )
							return null;
						var ticket = FormsAuthStatics.GetFormsAuthTicket( cookie );
						return ticket != null ? GetUser( int.Parse( ticket.Name ), false ) : null;
					},
					() => {
						var identity = HttpContext.Current.User.Identity;
						return identity.IsAuthenticated && identity.AuthenticationType == CertificateAuthenticationModule.CertificateAuthenticationType
							       ? GetUser( identity.Name )
							       : null;
					}
				}.Select( i => new Lazy<User>( i ) ).FirstOrDefault( i => i.Value != null );
			var user = userLazy != null ? userLazy.Value : null;

			if( ( user != null && user.Role.CanManageUsers ) || !ConfigurationStatics.IsLiveInstallation ) {
				var cookie = CookieStatics.GetCookie( UserImpersonationStatics.CookieName );
				if( cookie != null )
					return Tuple.Create( cookie.Value.Any() ? GetUser( int.Parse( cookie.Value ), false ) : null, Tuple.Create( user ) );
			}

			return Tuple.Create( user, (Tuple<User>)null );
		}
	}
}