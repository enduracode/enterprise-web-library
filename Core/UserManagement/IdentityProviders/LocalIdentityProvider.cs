using System;
using System.Linq;
using EnterpriseWebLibrary.Email;
using EnterpriseWebLibrary.Encryption;
using Tewl.InputValidation;

namespace EnterpriseWebLibrary.UserManagement.IdentityProviders {
	/// <summary>
	/// An identity provider that uses the system’s own user-management functionality.
	/// </summary>
	public class LocalIdentityProvider: IdentityProvider {
		public delegate void UserUpdaterMethod( int userId, int salt, byte[] saltedPassword, bool mustChangePassword );

		internal readonly string AdministratingCompanyName;
		internal readonly string LogInHelpInstructions;
		private readonly Func<string, ( User user, int salt, byte[] saltedPassword, bool mustChangePassword )?> passwordLogInUserGetter;
		private readonly Func<string, string, ( string subject, string bodyHtml )> passwordResetEmailGetter;
		private readonly Func<User, bool, string> postAuthenticationMethod;
		internal readonly int? AuthenticationTimeoutMinutes;
		internal readonly Action<Validator, string> PasswordValidationMethod;
		internal readonly UserUpdaterMethod UserUpdater;

		/// <summary>
		/// Creates a local identity provider.
		/// </summary>
		/// <param name="administratingCompanyName">The name of the company responsible for administrating the web site. Do not pass null.</param>
		/// <param name="logInHelpInstructions">The text explaining what to do if the user has trouble logging in. An example is "call 555-555-5555." or "talk to
		/// XXX." Do not pass null.</param>
		/// <param name="passwordLogInUserGetter">A function that takes an email address and returns the corresponding user object along with the user’s salt and
		/// salted password, or null if a user with that email address does not exist. Do not pass null. We recommend that you use case-insensitive comparison.
		/// </param>
		/// <param name="passwordResetEmailGetter">A function that takes a user’s email address and password and returns the subject and body of the message that
		/// will be sent to the user when a password reset is requested. The bodyHtml element must be HTML, so if needed call GetTextAsEncodedHtml() on plain text.
		/// </param>
		/// <param name="userUpdater">A method that takes a user ID and new data and updates the corresponding user. Do not pass null.</param>
		/// <param name="postAuthenticationMethod">Performs actions immediately after password authentication, which could include counting failed authentication
		/// attempts or preventing a user from logging in. Takes a user object and whether authentication was successful, and returns an error message if log-in
		/// should be prevented or the empty string otherwise. Do not use unless the system absolutely requires micromanagement of authentication behavior.</param>
		/// <param name="authenticationTimeoutMinutes">The authentication timeout. Pass null to use the default. Do not use unless the system absolutely requires
		/// micromanagement of authentication behavior.</param>
		/// <param name="passwordValidationMethod">Validates the specified password. Called when a user changes their password. Do not use unless the system
		/// absolutely requires micromanagement of authentication behavior.</param>
		public LocalIdentityProvider(
			string administratingCompanyName, string logInHelpInstructions,
			Func<string, ( User user, int salt, byte[] saltedPassword, bool mustChangePassword )?> passwordLogInUserGetter,
			Func<string, string, ( string subject, string bodyHtml )> passwordResetEmailGetter, UserUpdaterMethod userUpdater,
			Func<User, bool, string> postAuthenticationMethod = null, int? authenticationTimeoutMinutes = null,
			Action<Validator, string> passwordValidationMethod = null ) {
			AdministratingCompanyName = administratingCompanyName;
			LogInHelpInstructions = logInHelpInstructions;
			this.passwordLogInUserGetter = passwordLogInUserGetter;
			this.passwordResetEmailGetter = passwordResetEmailGetter;
			this.postAuthenticationMethod = postAuthenticationMethod;
			AuthenticationTimeoutMinutes = authenticationTimeoutMinutes;
			PasswordValidationMethod = passwordValidationMethod;
			UserUpdater = userUpdater;
		}

		internal string LogInUserWithPassword(
			string emailAddress, string password, string emailAddressErrorMessage, string passwordErrorMessage, out User user, out bool mustChangePassword ) {
			var userData = passwordLogInUserGetter( emailAddress );
			if( !userData.HasValue ) {
				user = null;
				mustChangePassword = false;
				return emailAddressErrorMessage;
			}
			user = userData.Value.user;
			mustChangePassword = userData.Value.mustChangePassword;

			var authenticationSuccessful = false;
			if( userData.Value.saltedPassword != null ) {
				// Trim the password if it is temporary; the user may have copied and pasted it from an email, which can add white space on the ends.
				var hashedPassword = new Password( userData.Value.mustChangePassword ? password.Trim() : password, userData.Value.salt ).ComputeSaltedHash();
				if( userData.Value.saltedPassword.SequenceEqual( hashedPassword ) )
					authenticationSuccessful = true;
			}

			if( postAuthenticationMethod != null ) {
				var errorMessage = postAuthenticationMethod( user, authenticationSuccessful );

				// Re-retrieve the user in case PostAuthenticate modified it.
				user = UserManagementStatics.SystemProvider.GetUser( user.UserId );

				if( errorMessage.Any() )
					return errorMessage;
			}

			return authenticationSuccessful ? "" : passwordErrorMessage;
		}

		/// <summary>
		/// Returns true if the given credentials correspond to a user and are correct.
		/// </summary>
		public bool UserCredentialsAreCorrect( string emailAddress, string password ) {
			var userData = passwordLogInUserGetter( emailAddress );
			return userData?.saltedPassword != null &&
			       userData.Value.saltedPassword.SequenceEqual( new Password( password, userData.Value.salt ).ComputeSaltedHash() );
		}


		// Password Reset

		internal bool PasswordResetEnabled {
			get {
				var email = passwordResetEmailGetter( "", "" );
				return email.subject.Any() || email.bodyHtml.Any();
			}
		}

		/// <summary>
		/// Resets the password of the user with the specified email address and sends a message with the new password to their email address. Returns the specified
		/// error message if a user with that email address does not exist, or the empty string otherwise.
		/// </summary>
		public string ResetAndSendPassword( string emailAddress, string emailAddressErrorMessage ) {
			var userData = passwordLogInUserGetter( emailAddress );
			if( !userData.HasValue )
				return emailAddressErrorMessage;
			ResetAndSendPassword( userData.Value.user.UserId );
			return "";
		}

		/// <summary>
		/// Resets the password of the given user and sends a message with the new password to their email address.
		/// </summary>
		public void ResetAndSendPassword( int userId ) {
			// reset the password
			var newPassword = new Password();
			UserUpdater( userId, newPassword.Salt, newPassword.ComputeSaltedHash(), true );

			// send the email
			SendPassword( UserManagementStatics.SystemProvider.GetUser( userId ).Email, newPassword.PasswordText );
		}

		internal void SendPassword( string emailAddress, string password ) {
			var email = passwordResetEmailGetter( emailAddress, password );
			var m = new EmailMessage { Subject = email.subject, BodyHtml = email.bodyHtml };
			m.ToAddresses.Add( new EmailAddress( emailAddress ) );
			EmailStatics.SendEmailWithDefaultFromAddress( m );
		}
	}
}