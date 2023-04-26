using System.Security.Cryptography;
using System.Text;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.Email;
using NodaTime;
using Tewl.InputValidation;

namespace EnterpriseWebLibrary.UserManagement.IdentityProviders;

/// <summary>
/// An identity provider that uses the system’s own user-management functionality.
/// </summary>
public class LocalIdentityProvider: IdentityProvider {
	/// <summary>
	/// Class for generating and hashing passwords.
	/// Code from http://www.aspheute.com/english/20040105.asp.
	/// </summary>
	internal class Password {
		private static int createRandomSalt() {
			var saltBytes = new Byte[ 4 ];
			var rng = new RNGCryptoServiceProvider();
			rng.GetBytes( saltBytes );

			return ( ( saltBytes[ 0 ] << 24 ) + ( saltBytes[ 1 ] << 16 ) + ( saltBytes[ 2 ] << 8 ) + saltBytes[ 3 ] );
		}

		private readonly string password;
		private readonly int salt;

		public int Salt => salt;

		/// <summary>
		/// Create a new password with the given text and randomly generated salt.
		/// </summary>
		public Password( string passwordText ): this( passwordText, createRandomSalt() ) {}

		/// <summary>
		/// Create a password from a stored salt and the given password text.
		/// </summary>
		public Password( string strPassword, int nSalt ) {
			password = strPassword;
			salt = nSalt;
		}

		public byte[] ComputeSaltedHash() {
			// Create a new salt
			var saltBytes = new Byte[ 4 ];
			saltBytes[ 0 ] = (byte)( salt >> 24 );
			saltBytes[ 1 ] = (byte)( salt >> 16 );
			saltBytes[ 2 ] = (byte)( salt >> 8 );
			saltBytes[ 3 ] = (byte)( salt );

			// Create Byte array of password string
			var encoder = new ASCIIEncoding();
			var secretBytes = encoder.GetBytes( password );

			// append the two arrays
			var toHash = new Byte[ secretBytes.Length + saltBytes.Length ];
			Array.Copy( secretBytes, 0, toHash, 0, secretBytes.Length );
			Array.Copy( saltBytes, 0, toHash, secretBytes.Length, saltBytes.Length );

			var sha1 = SHA1.Create();
			return sha1.ComputeHash( toHash );
		}
	}

	public delegate ( byte[] salt, byte[] hashedCode, Instant? expirationTime, byte? remainingAttemptCount, string destinationUrl ) LoginCodeGetterMethod(
		int userId );

	public delegate void PasswordUpdaterMethod( int userId, int salt, byte[] saltedPassword );

	public delegate void LoginCodeUpdaterMethod(
		int userId, byte[] salt, byte[] hashedCode, Instant? expirationTime, byte? remainingAttemptCount, string destinationUrl );

	internal delegate string AutoLogInPageUrlGetterMethod( string user, string code );

	internal delegate string ChangePasswordPageUrlGetterMethod( string destinationUrl );

	internal readonly string AdministratingOrganizationName;
	internal readonly string LogInHelpInstructions;
	private readonly Func<string, ( User user, int salt, byte[] saltedPassword )?> passwordLoginUserGetter;
	private readonly LoginCodeGetterMethod loginCodeGetter;
	private readonly Func<User, bool, bool?> postAuthenticationMethod;
	internal readonly int? AuthenticationTimeoutMinutes;
	internal readonly Action<Validator, string> PasswordValidationMethod;
	internal readonly PasswordUpdaterMethod PasswordUpdater;
	private readonly LoginCodeUpdaterMethod loginCodeUpdater;

	/// <summary>
	/// Creates a local identity provider.
	/// </summary>
	/// <param name="administratingOrganizationName">The name of the company/organization responsible for administrating the website. Do not pass null.</param>
	/// <param name="logInHelpInstructions">The text explaining what to do if the user has trouble logging in. An example is "call 555-555-5555." or "talk to
	/// XXX." Do not pass null.</param>
	/// <param name="passwordLoginUserGetter">A function that takes an email address and returns the corresponding user object along with the user’s salt and
	/// salted password, or null if a user with that email address does not exist. Do not pass null. We recommend that you use case-insensitive comparison.
	/// </param>
	/// <param name="loginCodeGetter">A function that takes a user ID and returns the corresponding user’s login-code data.</param>
	/// <param name="passwordUpdater">A method that takes a user ID and new password data and updates the corresponding user. Do not pass null.</param>
	/// <param name="loginCodeUpdater">A method that takes a user ID and new login-code data and updates the corresponding user. Do not pass null.</param>
	/// <param name="postAuthenticationMethod">Performs actions immediately after password or login-code authentication, which could include counting failed
	/// authentication attempts or preventing a user from logging in. Takes a user object and whether built-in authentication was successful, and returns true
	/// if authentication is successful, false if it failed for any reason, and null if it did not fail but is incomplete. Do not use unless the system
	/// absolutely requires micromanagement of authentication behavior.</param>
	/// <param name="authenticationTimeoutMinutes">The authentication timeout. Pass null to use the default. Do not use unless the system absolutely requires
	/// micromanagement of authentication behavior.</param>
	/// <param name="passwordValidationMethod">Validates the specified password. Called when a user changes their password. Do not use unless the system
	/// absolutely requires micromanagement of authentication behavior.</param>
	public LocalIdentityProvider(
		string administratingOrganizationName, string logInHelpInstructions, Func<string, ( User user, int salt, byte[] saltedPassword )?> passwordLoginUserGetter,
		LoginCodeGetterMethod loginCodeGetter, PasswordUpdaterMethod passwordUpdater, LoginCodeUpdaterMethod loginCodeUpdater,
		Func<User, bool, bool?> postAuthenticationMethod = null, int? authenticationTimeoutMinutes = null,
		Action<Validator, string> passwordValidationMethod = null ) {
		AdministratingOrganizationName = administratingOrganizationName;
		LogInHelpInstructions = logInHelpInstructions;
		this.passwordLoginUserGetter = passwordLoginUserGetter;
		this.loginCodeGetter = loginCodeGetter;
		this.postAuthenticationMethod = postAuthenticationMethod;
		AuthenticationTimeoutMinutes = authenticationTimeoutMinutes;
		PasswordValidationMethod = passwordValidationMethod;
		PasswordUpdater = passwordUpdater;
		this.loginCodeUpdater = loginCodeUpdater;
	}

	internal string LogInUserWithPassword( string emailAddress, string password, out User user, string errorMessage = "" ) {
		if( errorMessage.Length == 0 )
			errorMessage =
				"Login failed. Please check your email address and password. If you do not know your password, please set a new one using the button below.";

		user = null;

		var userData = passwordLoginUserGetter( emailAddress );
		if( !userData.HasValue )
			return errorMessage;

		var passwordCorrect = false;
		if( userData.Value.saltedPassword != null ) {
			var hashedPassword = new Password( password, userData.Value.salt ).ComputeSaltedHash();
			if( userData.Value.saltedPassword.SequenceEqual( hashedPassword ) )
				passwordCorrect = true;
		}

		bool? authenticationSuccessful = passwordCorrect;
		if( postAuthenticationMethod != null )
			authenticationSuccessful = postAuthenticationMethod( userData.Value.user, passwordCorrect );

		if( !passwordCorrect || authenticationSuccessful == false )
			return errorMessage;
		user = userData.Value.user;
		if( postAuthenticationMethod != null )
			// Re-retrieve the user in case postAuthenticationMethod modified it.
			user = UserManagementStatics.SystemProvider.GetUser( user.UserId );
		return authenticationSuccessful == true ? "" : null;
	}

	/// <summary>
	/// Returns true if the given credentials correspond to a user and are correct.
	/// </summary>
	public bool UserCredentialsAreCorrect( string emailAddress, string password ) {
		var userData = passwordLoginUserGetter( emailAddress );
		return userData?.saltedPassword != null && userData.Value.saltedPassword.SequenceEqual( new Password( password, userData.Value.salt ).ComputeSaltedHash() );
	}

	internal void SendLoginCode(
		string emailAddress, bool isPasswordReset, AutoLogInPageUrlGetterMethod autologInPageUrlGetter,
		ChangePasswordPageUrlGetterMethod changePasswordPageUrlGetter, string destinationUrl, int? newUserRoleId = null ) {
		var user = UserManagementStatics.SystemProvider.GetUser( emailAddress );
		if( user == null ) {
			if( !newUserRoleId.HasValue )
				return;
			user = UserManagementStatics.GetUser( UserManagementStatics.SystemProvider.InsertOrUpdateUser( null, emailAddress, newUserRoleId.Value, null ), true );
		}

		string code;
		var salt = new byte[ 16 ];
		const string numbers = "123456789";
		using( var rng = RandomNumberGenerator.Create() ) {
			var bytes = new byte[ 4 ];
			var codeBuilder = new StringBuilder();
			for( var i = 0; i < 6; i += 1 ) {
				rng.GetBytes( bytes );
				codeBuilder.Append( numbers[ (int)( BitConverter.ToUInt32( bytes, 0 ) % numbers.Length ) ] );
			}
			code = codeBuilder.ToString();

			rng.GetBytes( salt );
		}

		var codeDuration = Duration.FromMinutes( 10 );
		loginCodeUpdater(
			user.UserId,
			salt,
			getHashedLoginCode( code, salt ),
			SystemClock.Instance.GetCurrentInstant().Plus( codeDuration ),
			10,
			isPasswordReset ? changePasswordPageUrlGetter( destinationUrl ) : destinationUrl );

		var body = new StringBuilder();
		body.AppendLine(
			"<p><a href=\"{0}\" style=\"font-size: 1.2em;\">Click or tap here to log in automatically</a></p>".FormatWith(
				autologInPageUrlGetter( user.Email, code ) ) );
		body.AppendLine( "<p>or enter this code:<br><b style=\"font-size: 1.4em;\">{0} {1}</b></p>".FormatWith( code.Substring( 0, 3 ), code.Substring( 3, 3 ) ) );
		if( isPasswordReset )
			body.AppendLine(
				"<p>You will then be prompted to change your password to something you will remember, which you may use to log in from that point forward.</p>" );
		body.AppendLine(
			"<p>This login information is valid for {0}. If you need to log in later, please return to the page you were trying to access and send yourself another login email.</p>"
				.FormatWith( codeDuration.ToTimeSpan().ToConciseString() ) );
		body.AppendLine( "<p>Thank you,<br>{0}</p>".FormatWith( AdministratingOrganizationName.CapitalizeString() ) );

		var message = new EmailMessage
			{
				Subject = isPasswordReset
					          ? "Reset password for {0}".FormatWith( ConfigurationStatics.SystemDisplayName )
					          : "Log in to {0}".FormatWith( ConfigurationStatics.SystemDisplayName ),
				BodyHtml = body.ToString()
			};
		message.ToAddresses.Add( new EmailAddress( emailAddress ) );
		EmailStatics.SendEmailWithDefaultFromAddress( message );
	}

	internal string LogInUserWithCode( string emailAddress, string code, out User user, out string destinationUrl, string errorMessage = "" ) {
		if( errorMessage.Length == 0 )
			errorMessage = "The login code you entered is incorrect or has expired. Please check for typos. If there aren’t any, please send yourself another code.";

		user = null;
		destinationUrl = "";

		var userLocal = UserManagementStatics.SystemProvider.GetUser( emailAddress );
		if( userLocal == null )
			return errorMessage;

		var codeValid = true;

		var codeData = loginCodeGetter( userLocal.UserId );
		if( codeData.hashedCode == null )
			codeValid = false;
		else if( codeData.expirationTime.Value <= SystemClock.Instance.GetCurrentInstant() )
			codeValid = false;
		else if( codeData.remainingAttemptCount.Value == 0 )
			codeValid = false;
		else if( !codeData.hashedCode.SequenceEqual( getHashedLoginCode( code, codeData.salt ) ) ) {
			codeValid = false;
			loginCodeUpdater(
				userLocal.UserId,
				codeData.salt,
				codeData.hashedCode,
				codeData.expirationTime.Value,
				(byte)( codeData.remainingAttemptCount.Value - 1 ),
				codeData.destinationUrl );
		}

		bool? authenticationSuccessful = codeValid;
		if( postAuthenticationMethod != null ) {
			authenticationSuccessful = postAuthenticationMethod( userLocal, codeValid );

			// Re-retrieve the user in case postAuthenticationMethod modified it.
			userLocal = UserManagementStatics.SystemProvider.GetUser( userLocal.UserId );
		}

		if( !codeValid || authenticationSuccessful == false )
			return errorMessage;

		loginCodeUpdater( userLocal.UserId, null, null, null, null, "" );

		user = userLocal;
		destinationUrl = codeData.destinationUrl;
		return authenticationSuccessful == true ? "" : null;
	}

	private byte[] getHashedLoginCode( string code, byte[] salt ) {
		using var pbkdf2 = new Rfc2898DeriveBytes( code, salt, 10000, HashAlgorithmName.SHA1 );

		// see https://security.stackexchange.com/a/167403/20277
		return pbkdf2.GetBytes( 20 );
	}
}