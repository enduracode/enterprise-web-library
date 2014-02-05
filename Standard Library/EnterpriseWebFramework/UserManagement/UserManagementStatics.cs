using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using RedStapler.StandardLibrary.Email;
using RedStapler.StandardLibrary.Encryption;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.Validation;
using RedStapler.StandardLibrary.WebSessionState;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement {
	/// <summary>
	/// Provides useful constants and methods pertaining to user management.
	/// </summary>
	public static class UserManagementStatics {
		private const string providerName = "UserManagement";
		private const string testCookieName = "TestCookie";

		/// <summary>
		/// The idle time required for a session to be erased.
		/// </summary>
		public static readonly TimeSpan SessionDuration = TimeSpan.FromHours( 10 );

		/// <summary>
		/// Do not use directly. Use <see cref="SystemProvider"/>.
		/// </summary>
		private static SystemUserManagementProvider provider;

		internal static void Init( Type systemLogicType ) {
			provider = StandardLibraryMethods.GetSystemLibraryProvider( systemLogicType, providerName ) as SystemUserManagementProvider;
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
					throw StandardLibraryMethods.CreateProviderNotFoundException( providerName );
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

		/// <summary>
		/// Gets an email address form item for use on log-in pages. The validation sets this data value to the post back value of the text box, if valid, or adds
		/// the specified error message to the form item.
		/// </summary>
		public static FormItem<EwfTextBox> GetEmailAddressFormItem( this DataValue<string> emailAddress, string label, string errorMessage, ValidationList vl ) {
			return FormItem.Create( label,
			                        new EwfTextBox( "" ),
			                        validationGetter:
				                        control =>
				                        new Validation(
					                        ( pbv, validator ) =>
					                        emailAddress.Value =
					                        validator.GetEmailAddress( new ValidationErrorHandler( ( v, ec ) => v.NoteErrorAndAddMessage( errorMessage ) ),
					                                                   control.GetPostBackValue( pbv ),
					                                                   false ),
					                        vl ) );
		}

		/// <summary>
		/// Resets the password of the user with the specified email address and sends a message with the new password to their email address. Do not call if the
		/// system does not implement the forms authentication capable user management provider.
		/// </summary>
		public static void ResetAndSendPassword( string validatedEmailAddress, string emailAddressErrorMessage ) {
			var user = GetUser( validatedEmailAddress );
			if( user == null )
				throw new DataModificationException( emailAddressErrorMessage );
			ResetAndSendPassword( user.UserId );
		}

		/// <summary>
		/// Resets the password of the given user and sends a message with the new password to their email address. Do not call if the system does not implement the
		/// forms authentication capable user management provider.
		/// </summary>
		public static void ResetAndSendPassword( int userId ) {
			var sp = SystemProvider as FormsAuthCapableUserManagementProvider;
			User user = sp.GetUser( userId );

			// reset the password
			var newPassword = new Password();
			sp.InsertOrUpdateUser( userId, user.Email, newPassword.Salt, newPassword.ComputeSaltedHash(), user.Role.RoleId, user.LastRequestDateTime, true );

			// send the email
			SendPassword( user.Email, newPassword.PasswordText );
		}

		internal static void SendPassword( string emailAddress, string password ) {
			string subject;
			string body;
			( SystemProvider as FormsAuthCapableUserManagementProvider ).GetPasswordResetParams( emailAddress, password, out subject, out body );
			var m = new EmailMessage { Subject = subject, BodyHtml = body.GetTextAsEncodedHtml() };
			m.ToAddresses.Add( new EmailAddress( emailAddress ) );
			AppTools.SendEmailWithDefaultFromAddress( m );
		}

		private static void setUpClientSideLogicForLogIn( DataValue<string> utcOffset, ValidationList vl ) {
			EwfPage.Instance.PreRender += delegate { setCookie( testCookieName, "No data" ); };

			Func<PostBackValueDictionary, string> utcOffsetHiddenFieldValueGetter; // unused
			Func<string> utcOffsetHiddenFieldClientIdGetter;
			EwfHiddenField.Create( "", postBackValue => utcOffset.Value = postBackValue, vl, out utcOffsetHiddenFieldValueGetter, out utcOffsetHiddenFieldClientIdGetter );
			EwfPage.Instance.PreRender +=
				delegate {
					EwfPage.Instance.ClientScript.RegisterOnSubmitStatement( typeof( UserManagementStatics ),
					                                                         "formSubmitEventHandler",
					                                                         "getClientUtcOffset( '" + utcOffsetHiddenFieldClientIdGetter() + "' );" );
				};
		}

		/// <summary>
		/// Sets up client-side logic for user log-in and returns a modification method that logs in a user. Do not call if the system does not implement the
		/// forms-authentication-capable user-management provider.
		/// </summary>
		public static Func<FormsAuthCapableUser> GetLogInMethod( DataValue<string> emailAddress, DataValue<string> password, string emailAddressErrorMessage,
		                                                         string passwordErrorMessage, ValidationList vl ) {
			var utcOffset = new DataValue<string>();
			setUpClientSideLogicForLogIn( utcOffset, vl );

			return () => {
				var errors = new List<string>();

				var formsAuthCapableUserManagementProvider = ( SystemProvider as FormsAuthCapableUserManagementProvider );
				var user = formsAuthCapableUserManagementProvider.GetUser( emailAddress.Value );
				if( user != null ) {
					var authenticationSuccessful = false;
					if( user.SaltedPassword != null ) {
						// Trim the password if it is temporary; the user may have copied and pasted it from an email, which can add white space on the ends.
						var hashedPassword = new Password( user.MustChangePassword ? password.Value.Trim() : password.Value, user.Salt ).ComputeSaltedHash();
						if( user.SaltedPassword.SequenceEqual( hashedPassword ) )
							authenticationSuccessful = true;
					}

					var strictProvider = SystemProvider as StrictFormsAuthUserManagementProvider;
					if( strictProvider != null ) {
						strictProvider.PostAuthenticate( user, authenticationSuccessful );

						// Re-retrieve the user in case PostAuthenticate modified it.
						user = formsAuthCapableUserManagementProvider.GetUser( user.UserId );
					}

					if( authenticationSuccessful )
						setFormsAuthCookieAndUser( user );
					else
						errors.Add( passwordErrorMessage );
				}
				else
					errors.Add( emailAddressErrorMessage );

				errors.AddRange( verifyTestCookie() );
				addStatusMessageIfClockNotSynchronized( utcOffset );

				if( errors.Any() )
					throw new DataModificationException( errors.ToArray() );
				return user;
			};
		}

		/// <summary>
		/// PRE: SystemProvider is a FormsAuthCapableUserManagementProvider.
		/// Returns true if the given credentials correspond to a user and are correct.
		/// </summary>
		public static bool UserCredentialsAreCorrect( string userEmailAddress, string providedPassword ) {
			// NOTE: With the exception of the password trimming, this is similar to the logic in GetLogInMethod.
			var user = ( SystemProvider as FormsAuthCapableUserManagementProvider ).GetUser( userEmailAddress );
			return user != null && user.SaltedPassword != null && user.SaltedPassword.SequenceEqual( new Password( providedPassword, user.Salt ).ComputeSaltedHash() );
		}

		/// <summary>
		/// Sets up client-side logic for user log-in and returns a modification method that logs in the specified user. Do not call if the system does not
		/// implement the forms-authentication-capable user-management provider.
		/// This method should be called in LoadData. The method returned should be called in an event handler.
		/// </summary>
		public static Action<int> GetSpecifiedUserLogInMethod( ValidationList vl ) {
			var utcOffset = new DataValue<string>();
			setUpClientSideLogicForLogIn( utcOffset, vl );

			return userId => {
				var user = ( SystemProvider as FormsAuthCapableUserManagementProvider ).GetUser( userId );
				setFormsAuthCookieAndUser( user );

				var errors = new List<string>();
				errors.AddRange( verifyTestCookie() );
				addStatusMessageIfClockNotSynchronized( utcOffset );
				if( errors.Any() )
					throw new DataModificationException( errors.ToArray() );
			};
		}

		private static void setFormsAuthCookieAndUser( FormsAuthCapableUser user ) {
			var strictProvider = SystemProvider as StrictFormsAuthUserManagementProvider;

			// If the user's role requires enhanced security, require re-authentication every 12 minutes. Otherwise, make it the same as a session timeout.
			var authenticationDuration = strictProvider != null && strictProvider.AuthenticationTimeoutInMinutes.HasValue
				                             ? TimeSpan.FromMinutes( strictProvider.AuthenticationTimeoutInMinutes.Value )
				                             : user.Role.RequiresEnhancedSecurity ? TimeSpan.FromMinutes( 12 ) : SessionDuration;

			var ticket = new FormsAuthenticationTicket( user.UserId.ToString(), false /*meaningless*/, (int)authenticationDuration.TotalMinutes );
			setFormsAuthCookie( ticket );

			AppRequestState.Instance.SetUser( user );
		}

		private static void setFormsAuthCookie( FormsAuthenticationTicket ticket ) {
			setCookie( formsAuthCookieName, FormsAuthentication.Encrypt( ticket ) );
		}

		private static void setCookie( string name, string value ) {
			AppRequestState.AddNonTransactionalModificationMethod(
				() =>
				HttpContext.Current.Response.Cookies.Add( new HttpCookie( name, value )
					{
						Path = NetTools.GetAppCookiePath(),
						Secure = EwfApp.SupportsSecureConnections,
						HttpOnly = true
					} ) );
		}

		private static string[] verifyTestCookie() {
			return HttpContext.Current.Request.Cookies[ testCookieName ] == null ? new[] { Translation.YourBrowserHasCookiesDisabled } : new string[ 0 ];
		}

		private static void addStatusMessageIfClockNotSynchronized( DataValue<string> utcOffset ) {
			try {
				// IE uses a "UTC" suffix and Firefox uses a "GMT" suffix.  For our purposes, they are the same thing. Ironically, Microsoft fails to parse the time
				// generated by its own product, so we convert it to be the same as the time Firefox gives, which parses fine.
				var clockDifference = DateTime.Parse( utcOffset.Value.Replace( "UTC", "GMT" ) ) - DateTime.Now;

				if( Math.Abs( clockDifference.TotalMinutes ) > 5 ) {
					EwfPage.AddStatusMessage( StatusMessageType.Warning,
					                          Translation.YourClockIsWrong + " " + DateTime.Now.ToShortTimeString() + " " +
					                          ( TimeZone.CurrentTimeZone.IsDaylightSavingTime( DateTime.Now )
						                            ? TimeZone.CurrentTimeZone.DaylightName
						                            : TimeZone.CurrentTimeZone.StandardName ) + "." );
				}
			}
			catch {} // NOTE: Figure out why the date time field passed from javascript might be empty, and get rid of this catch
		}

		/// <summary>
		/// Do not call if the system does not implement the forms authentication capable user management provider.
		/// </summary>
		public static void LogOutUser() {
			clearFormsAuthCookie();
			AppRequestState.Instance.SetUser( null );
		}

		private static void clearFormsAuthCookie() {
			AppRequestState.AddNonTransactionalModificationMethod(
				() =>
				HttpContext.Current.Response.Cookies.Add( new HttpCookie( formsAuthCookieName ) { Path = NetTools.GetAppCookiePath(), Expires = DateTime.Now.AddDays( -1 ) } ) );
		}

		/// <summary>
		/// Ensures that the specified data values contain identical, valid password values.
		/// </summary>
		public static void ValidatePassword( Validator validator, DataValue<string> password, DataValue<string> passwordAgain ) {
			if( password.Value != passwordAgain.Value )
				validator.NoteErrorAndAddMessage( "Passwords do not match." );
			else {
				var strictProvider = SystemProvider as StrictFormsAuthUserManagementProvider;
				if( strictProvider != null )
					strictProvider.ValidatePassword( validator, password.Value );
				else if( password.Value.Length < 7 )
					validator.NoteErrorAndAddMessage( "Passwords must be at least 7 characters long." );
			}
		}

		internal static User GetUserFromRequest() {
			var cookie = HttpContext.Current.Request.Cookies[ formsAuthCookieName ];
			if( cookie != null ) {
				var ticket = getFormsAuthTicket( cookie );
				if( ticket != null )
					return GetUser( int.Parse( ticket.Name ), false );
			}

			var identity = HttpContext.Current.User.Identity;
			if( identity.IsAuthenticated && identity.AuthenticationType == CertificateAuthenticationModule.CertificateAuthenticationType )
				return GetUser( identity.Name );

			return null;
		}

		internal static void UpdateFormsAuthCookieIfNecessary() {
			var cookie = HttpContext.Current.Request.Cookies[ formsAuthCookieName ];
			if( cookie == null )
				return;

			var ticket = getFormsAuthTicket( cookie );
			if( ticket != null ) {
				var newTicket = FormsAuthentication.RenewTicketIfOld( ticket );
				if( newTicket != ticket )
					setFormsAuthCookie( newTicket );
			}
			else
				clearFormsAuthCookie();
		}

		private static FormsAuthenticationTicket getFormsAuthTicket( HttpCookie cookie ) {
			FormsAuthenticationTicket ticket = null;
			try {
				ticket = FormsAuthentication.Decrypt( cookie.Value );
			}
			catch {}
			return ticket != null && !ticket.Expired ? ticket : null;
		}

		private static string formsAuthCookieName { get { return "User"; } }
	}
}