using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using EnterpriseWebLibrary.Email;
using EnterpriseWebLibrary.Encryption;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;
using EnterpriseWebLibrary.InputValidation;
using EnterpriseWebLibrary.WebSessionState;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement {
	/// <summary>
	/// Statics related to forms authentication.
	/// </summary>
	public static class FormsAuthStatics {
		private const string testCookieName = "TestCookie";

		/// <summary>
		/// The idle time required for a session to be erased.
		/// </summary>
		public static readonly TimeSpan SessionDuration = TimeSpan.FromHours( 10 );

		private static Func<SystemUserManagementProvider> providerGetter;

		internal static void Init( Func<SystemUserManagementProvider> providerGetter ) {
			FormsAuthStatics.providerGetter = providerGetter;
		}

		/// <summary>
		/// EWL use only.
		/// </summary>
		public static bool FormsAuthEnabled { get { return providerGetter() is FormsAuthCapableUserManagementProvider; } }

		/// <summary>
		/// EWL use only.
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


		// Adding a New User

		/// <summary>
		/// Gets password and "password again" form items. The validation sets this data value to the provided password, and ensures that the two form items contain
		/// identical, valid passwords.
		/// </summary>
		public static IReadOnlyCollection<FormItem> GetPasswordModificationFormItems(
			this DataValue<string> password, Unit? textBoxWidth = null, FormItemLabel firstLabel = null, FormItemLabel secondLabel = null ) {
			var passwordAgain = new DataValue<string>();
			var passwordAgainFormItem = FormItem.Create(
				secondLabel ?? "Password again",
				new EwfTextBox( "", masksCharacters: true, disableBrowserAutoComplete: true ),
				validationGetter: control => new EwfValidation( ( pbv, v ) => passwordAgain.Value = control.GetPostBackValue( pbv ) ) );
			if( textBoxWidth.HasValue )
				passwordAgainFormItem.Control.Width = textBoxWidth.Value;

			var passwordFormItem = FormItem.Create(
				firstLabel ?? "Password",
				new EwfTextBox( "", masksCharacters: true, disableBrowserAutoComplete: true ),
				validationGetter: control => new EwfValidation(
					                             ( pbv, v ) => {
						                             password.Value = control.GetPostBackValue( pbv );
						                             if( password.Value != passwordAgain.Value )
							                             v.NoteErrorAndAddMessage( "Passwords do not match." );
						                             else {
							                             var strictProvider = SystemProvider as StrictFormsAuthUserManagementProvider;
							                             if( strictProvider != null )
								                             strictProvider.ValidatePassword( v, password.Value );
							                             else if( password.Value.Length < 7 )
								                             v.NoteErrorAndAddMessage( "Passwords must be at least 7 characters long." );
						                             }
					                             } ) );
			if( textBoxWidth.HasValue )
				passwordFormItem.Control.Width = textBoxWidth.Value;

			return new[] { passwordFormItem, passwordAgainFormItem };
		}


		// Log-In

		/// <summary>
		/// Gets an email address form item for use on log-in pages. The validation sets this data value to the post back value of the text box, if valid, or adds
		/// the specified error message to the form item.
		/// </summary>
		public static FormItem<EwfTextBox> GetEmailAddressFormItem( this DataValue<string> emailAddress, FormItemLabel label, string errorMessage ) {
			return FormItem.Create(
				label,
				new EwfTextBox( "" ),
				validationGetter:
					control =>
					new EwfValidation(
						( pbv, validator ) =>
						emailAddress.Value =
						validator.GetEmailAddress( new ValidationErrorHandler( ( v, ec ) => v.NoteErrorAndAddMessage( errorMessage ) ), control.GetPostBackValue( pbv ), false ) ) );
		}

		/// <summary>
		/// Returns log-in hidden fields and a modification method that logs in a user. Also sets up client-side logic for user log-in. Do not call if the system
		/// does not implement the forms-authentication-capable user-management provider.
		/// </summary>
		public static Tuple<IReadOnlyCollection<EtherealComponent>, Func<FormsAuthCapableUser>> GetLogInHiddenFieldsAndMethod(
			DataValue<string> emailAddress, DataValue<string> password, string emailAddressErrorMessage, string passwordErrorMessage ) {
			var utcOffset = new DataValue<string>();
			var hiddenFields = getLogInHiddenFieldsAndSetUpClientSideLogic( utcOffset );

			return Tuple.Create(
				hiddenFields,
				new Func<FormsAuthCapableUser>(
					() => {
						var errors = new List<string>();

						var user = SystemProvider.GetUser( emailAddress.Value );
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
								user = SystemProvider.GetUser( user.UserId );
							}

							if( authenticationSuccessful )
								SetFormsAuthCookieAndUser( user );
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
					} ) );
		}

		/// <summary>
		/// PRE: SystemProvider is a FormsAuthCapableUserManagementProvider.
		/// Returns true if the given credentials correspond to a user and are correct.
		/// </summary>
		public static bool UserCredentialsAreCorrect( string userEmailAddress, string providedPassword ) {
			// NOTE: With the exception of the password trimming, this is similar to the logic in GetLogInMethod.
			var user = SystemProvider.GetUser( userEmailAddress );
			return user != null && user.SaltedPassword != null && user.SaltedPassword.SequenceEqual( new Password( providedPassword, user.Salt ).ComputeSaltedHash() );
		}

		/// <summary>
		/// Returns log-in hidden fields and a modification method that logs in the specified user. Also sets up client-side logic for user log-in. Do not call if
		/// the system does not implement the forms-authentication-capable user-management provider.
		/// </summary>
		public static Tuple<IReadOnlyCollection<EtherealComponent>, Action<int>> GetLogInHiddenFieldsAndSpecifiedUserLogInMethod( Control etherealControlParent ) {
			var utcOffset = new DataValue<string>();
			var hiddenFields = getLogInHiddenFieldsAndSetUpClientSideLogic( utcOffset );

			return Tuple.Create(
				hiddenFields,
				new Action<int>(
					userId => {
						var user = SystemProvider.GetUser( userId );
						SetFormsAuthCookieAndUser( user );

						var errors = new List<string>();
						errors.AddRange( verifyTestCookie() );
						addStatusMessageIfClockNotSynchronized( utcOffset );
						if( errors.Any() )
							throw new DataModificationException( errors.ToArray() );
					} ) );
		}

		private static IReadOnlyCollection<EtherealComponent> getLogInHiddenFieldsAndSetUpClientSideLogic( DataValue<string> utcOffset ) {
			EwfPage.Instance.PreRender += delegate { setCookie( testCookieName, "No data" ); };

			HiddenFieldId utcOffsetHiddenFieldId = new HiddenFieldId();
			var utcOffsetHiddenField = new EwfHiddenField( "", ( postBackValue, validator ) => utcOffset.Value = postBackValue.Value, id: utcOffsetHiddenFieldId );
			EwfPage.Instance.PreRender += delegate {
				if( utcOffsetHiddenFieldId.Id.Length == 0 )
					throw new ApplicationException( "The log-in hidden fields must be on the page." );
				EwfPage.Instance.ClientScript.RegisterOnSubmitStatement(
					typeof( UserManagementStatics ),
					"formSubmitEventHandler",
					"getClientUtcOffset( '{0}' )".FormatWith( utcOffsetHiddenFieldId.Id ) );
			};

			return utcOffsetHiddenField.PageComponent.ToSingleElementArray();
		}

		/// <summary>
		/// MVC and private use only.
		/// </summary>
		public static void SetFormsAuthCookieAndUser( FormsAuthCapableUser user ) {
			if( AppRequestState.Instance.ImpersonatorExists )
				UserImpersonationStatics.SetCookie( user );
			else {
				var strictProvider = SystemProvider as StrictFormsAuthUserManagementProvider;

				// If the user's role requires enhanced security, require re-authentication every 12 minutes. Otherwise, make it the same as a session timeout.
				var authenticationDuration = strictProvider != null && strictProvider.AuthenticationTimeoutInMinutes.HasValue
					                             ? TimeSpan.FromMinutes( strictProvider.AuthenticationTimeoutInMinutes.Value )
					                             : user.Role.RequiresEnhancedSecurity ? TimeSpan.FromMinutes( 12 ) : SessionDuration;

				var ticket = new FormsAuthenticationTicket( user.UserId.ToString(), false /*meaningless*/, (int)authenticationDuration.TotalMinutes );
				AppRequestState.AddNonTransactionalModificationMethod( () => setFormsAuthCookie( ticket ) );
			}
			AppRequestState.Instance.SetUser( user );
		}

		private static void setFormsAuthCookie( FormsAuthenticationTicket ticket ) {
			setCookie( FormsAuthCookieName, FormsAuthentication.Encrypt( ticket ) );
		}

		private static void setCookie( string name, string value ) {
			CookieStatics.SetCookie( name, value, null, EwfConfigurationStatics.AppSupportsSecureConnections, true );
		}

		private static string[] verifyTestCookie() {
			return CookieStatics.GetCookie( testCookieName ) == null ? new[] { Translation.YourBrowserHasCookiesDisabled } : new string[ 0 ];
		}

		private static void addStatusMessageIfClockNotSynchronized( DataValue<string> utcOffset ) {
			try {
				// IE uses a "UTC" suffix and Firefox uses a "GMT" suffix.  For our purposes, they are the same thing. Ironically, Microsoft fails to parse the time
				// generated by its own product, so we convert it to be the same as the time Firefox gives, which parses fine.
				var clockDifference = DateTime.Parse( utcOffset.Value.Replace( "UTC", "GMT" ) ) - DateTime.Now;

				if( Math.Abs( clockDifference.TotalMinutes ) > 5 )
					EwfPage.AddStatusMessage(
						StatusMessageType.Warning,
						Translation.YourClockIsWrong + " " + DateTime.Now.ToShortTimeString() + " " +
						( TimeZone.CurrentTimeZone.IsDaylightSavingTime( DateTime.Now ) ? TimeZone.CurrentTimeZone.DaylightName : TimeZone.CurrentTimeZone.StandardName ) + "." );
			}
			catch {} // NOTE: Figure out why the date time field passed from javascript might be empty, and get rid of this catch
		}


		// Cookie Updating

		internal static void UpdateFormsAuthCookieIfNecessary() {
			var cookie = CookieStatics.GetCookie( FormsAuthCookieName );
			if( cookie == null )
				return;

			var ticket = GetFormsAuthTicket( cookie );
			if( ticket != null ) {
				var newTicket = FormsAuthentication.RenewTicketIfOld( ticket );
				if( newTicket != ticket )
					setFormsAuthCookie( newTicket );
			}
			else
				clearFormsAuthCookie();
		}

		internal static FormsAuthenticationTicket GetFormsAuthTicket( HttpCookie cookie ) {
			FormsAuthenticationTicket ticket = null;
			try {
				ticket = FormsAuthentication.Decrypt( cookie.Value );
			}
			catch {}
			return ticket != null && !ticket.Expired ? ticket : null;
		}


		// Log-Out

		/// <summary>
		/// Do not call if the system does not implement the forms authentication capable user management provider.
		/// </summary>
		public static void LogOutUser() {
			if( AppRequestState.Instance.ImpersonatorExists )
				UserImpersonationStatics.SetCookie( null );
			else
				AppRequestState.AddNonTransactionalModificationMethod( clearFormsAuthCookie );
			AppRequestState.Instance.SetUser( null );
		}

		private static void clearFormsAuthCookie() {
			CookieStatics.ClearCookie( FormsAuthCookieName );
		}

		internal static string FormsAuthCookieName { get { return "User"; } }


		// Password Reset

		/// <summary>
		/// EWL use only.
		/// </summary>
		public static bool PasswordResetEnabled {
			get {
				string subject;
				string bodyHtml;
				SystemProvider.GetPasswordResetParams( "", "", out subject, out bodyHtml );
				return subject.Any() || bodyHtml.Any();
			}
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
			User user = SystemProvider.GetUser( userId );

			// reset the password
			var newPassword = new Password();
			SystemProvider.InsertOrUpdateUser( userId, user.Email, user.Role.RoleId, user.LastRequestDateTime, newPassword.Salt, newPassword.ComputeSaltedHash(), true );

			// send the email
			SendPassword( user.Email, newPassword.PasswordText );
		}

		internal static void SendPassword( string emailAddress, string password ) {
			string subject;
			string bodyHtml;
			SystemProvider.GetPasswordResetParams( emailAddress, password, out subject, out bodyHtml );
			var m = new EmailMessage { Subject = subject, BodyHtml = bodyHtml };
			m.ToAddresses.Add( new EmailAddress( emailAddress ) );
			EmailStatics.SendEmailWithDefaultFromAddress( m );
		}
	}
}