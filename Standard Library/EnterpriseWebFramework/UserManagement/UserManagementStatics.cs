using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Security;
using RedStapler.StandardLibrary.DataAccess;
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
		private const string utcOffsetHiddenFieldName = "utcOffset";

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

		// NOTE: Why are some things using these methods and other things calling GetUser directly? The things not using them want to assume that the provider is
		// FormsAuthCapable, but calling the provider directly is a crappy solution, so fix it somehow.
		// NOTE: It seems like we could cache a collection of Roles and have users just take a roleId, and look up the object ourselves. This would save the apps
		// creating the role object, and all save the extra database query.  But where would we do this?
		/// <summary>
		/// Standard Library use only.
		/// </summary>
		public static List<User> GetUsers( DBConnection cn ) {
			if( SystemProvider is FormsAuthCapableUserManagementProvider )
				return ( SystemProvider as FormsAuthCapableUserManagementProvider ).GetUsers().ConvertAll( input => input as User );
			if( SystemProvider is ExternalAuthUserManagementProvider )
				return ( SystemProvider as ExternalAuthUserManagementProvider ).GetUsers().ConvertAll( input => input as User );
			throw new ApplicationException( "Unknown user management setup type." );
		}

		// NOTE: Give this method a boolean parameter that determines whether to blow up if there is no user. Audit all providers and make sure they are not using .Single().
		/// <summary>
		/// Standard Library use only.
		/// </summary>
		public static User GetUser( DBConnection cn, int userId ) {
			if( SystemProvider is FormsAuthCapableUserManagementProvider )
				return ( SystemProvider as FormsAuthCapableUserManagementProvider ).GetUser( userId );
			if( SystemProvider is ExternalAuthUserManagementProvider )
				return ( SystemProvider as ExternalAuthUserManagementProvider ).GetUser( userId );
			throw new ApplicationException( "Unknown user management setup type." );
		}

		// NOTE: Audit all providers and make sure they are not using .Single() to implement these methods.
		/// <summary>
		/// Standard Library use only.
		/// </summary>
		public static User GetUser( DBConnection cn, string emailAddress ) {
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
			var user = GetUser( AppRequestState.PrimaryDatabaseConnection, validatedEmailAddress );
			if( user == null )
				throw new EwfException( emailAddressErrorMessage );
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

		/// <summary>
		/// This method supports LogInUser and LogInSpecifiedUser. Do not call if the system does not implement the forms authentication capable user management
		/// provider.
		/// </summary>
		public static void SetUpClientSideLogicForLogInPostBack() {
			EwfPage.Instance.PreRender += delegate { setCookie( testCookieName, "No data" ); };

			EwfPage.Instance.ClientScript.RegisterHiddenField( utcOffsetHiddenFieldName, "" );
			EwfPage.Instance.ClientScript.RegisterOnSubmitStatement( typeof( UserManagementStatics ),
			                                                         "formSubmitEventHandler",
			                                                         "getClientUtcOffset( '" + utcOffsetHiddenFieldName + "' );" );
		}

		/// <summary>
		/// Only call this method if LoadData contains a call to SetUpClientSideLogicForLogInPostBack. Do not call if the system does not implement the forms
		/// authentication capable user management provider.
		/// </summary>
		public static FormsAuthCapableUser LogInUser( DataValue<string> emailAddress, DataValue<string> password, string emailAddressErrorMessage,
		                                              string passwordErrorMessage ) {
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

						// This system wants to avoid a forced migration and because of this we're adding an exception here.
						// NOTE: Remove after 30 September 2013.
					else {
						var asciiEncoding = new ASCIIEncoding();
						if( AppTools.SystemName == "Health Alliance Enterprise System" &&
						    user.SaltedPassword.SequenceEqual( asciiEncoding.GetBytes( asciiEncoding.GetString( hashedPassword ) ) ) ) {
							authenticationSuccessful = true;

							// Migrate the user's account to use the new hash.
							formsAuthCapableUserManagementProvider.InsertOrUpdateUser( user.UserId,
							                                                           user.Email,
							                                                           user.Salt,
							                                                           hashedPassword,
							                                                           user.Role.RoleId,
							                                                           user.LastRequestDateTime,
							                                                           user.MustChangePassword );
						}
					}
				}

				var strictProvider = SystemProvider as StrictFormsAuthUserManagementProvider;
				if( strictProvider != null ) {
					strictProvider.PostAuthenticate( user, authenticationSuccessful );

					// Re-retrieve the user in case PostAuthenticate modified it.
					user = formsAuthCapableUserManagementProvider.GetUser( user.UserId );
				}

				if( authenticationSuccessful )
					setCookieAndUser( user );
				else
					errors.Add( passwordErrorMessage );
			}
			else
				errors.Add( emailAddressErrorMessage );

			errors.AddRange( verifyTestCookie() );
			addStatusMessageIfClockNotSynchronized();

			if( errors.Any() )
				throw new EwfException( errors.ToArray() );
			return user;
		}

		/// <summary>
		/// PRE: SystemProvider is a FormsAuthCapableUserManagementProvider.
		/// Returns true if the given credentials correspond to a user and are correct.
		/// </summary>
		public static bool UserCredentialsAreCorrect( DBConnection cn, string userEmailAddress, string providedPassword ) {
			// NOTE: Could share this with line 160 above. Not sure about the password trimming, though.
			var user = ( SystemProvider as FormsAuthCapableUserManagementProvider ).GetUser( userEmailAddress );
			return user != null && user.SaltedPassword != null && user.SaltedPassword.SequenceEqual( new Password( providedPassword, user.Salt ).ComputeSaltedHash() );
		}

		/// <summary>
		/// Only call this method if LoadData contains a call to SetUpClientSideLogicForLogInPostBack. Do not call if the system does not implement the forms
		/// authentication capable user management provider.
		/// </summary>
		public static void LogInSpecifiedUser( int userId ) {
			var user = ( SystemProvider as FormsAuthCapableUserManagementProvider ).GetUser( userId );
			setCookieAndUser( user );

			var errors = new List<string>();
			errors.AddRange( verifyTestCookie() );
			addStatusMessageIfClockNotSynchronized();
			if( errors.Any() )
				throw new EwfException( errors.ToArray() );
		}

		private static void setCookieAndUser( FormsAuthCapableUser user ) {
			AppRequestState.AddNonTransactionalModificationMethod( () => {
				var strictProvider = SystemProvider as StrictFormsAuthUserManagementProvider;

				// If the user's role requires enhanced security, require re-authentication every 12 minutes. Otherwise, make it the same as a session timeout.
				var authenticationDuration = strictProvider != null && strictProvider.AuthenticationTimeoutInMinutes.HasValue
					                             ? TimeSpan.FromMinutes( strictProvider.AuthenticationTimeoutInMinutes.Value )
					                             : user.Role.RequiresEnhancedSecurity ? TimeSpan.FromMinutes( 12 ) : SessionDuration;

				var ticket = new FormsAuthenticationTicket( user.UserId.ToString(), true /*persistent*/, (int)authenticationDuration.TotalMinutes );
				setCookie( FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt( ticket ) );
			} );

			AppRequestState.Instance.SetUser( user );
		}

		private static void setCookie( string name, string value ) {
			HttpContext.Current.Response.Cookies.Add( new HttpCookie( name, value ) { Secure = EwfApp.SupportsSecureConnections, HttpOnly = true } );
		}

		private static string[] verifyTestCookie() {
			return HttpContext.Current.Request.Cookies[ testCookieName ] == null ? new[] { Translation.YourBrowserHasCookiesDisabled } : new string[ 0 ];
		}

		private static void addStatusMessageIfClockNotSynchronized() {
			try {
				// IE uses a "UTC" suffix and Firefox uses a "GMT" suffix.  For our purposes, they are the same thing. Ironically, Microsoft fails to parse the time
				// generated by its own product, so we convert it to be the same as the time Firefox gives, which parses fine.
				var clockDifference = DateTime.Parse( HttpContext.Current.Request.Form[ utcOffsetHiddenFieldName ].Replace( "UTC", "GMT" ) ) - DateTime.Now;

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
			FormsAuthentication.SignOut();
			AppRequestState.Instance.SetUser( null );
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
	}
}