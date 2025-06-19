using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using EnterpriseWebLibrary.Caching;
using EnterpriseWebLibrary.Email;
using EnterpriseWebLibrary.SystemSpecificLogic;
using Humanizer;
using JetBrains.Annotations;
using NodaTime;
using Tewl.InputValidation;

namespace EnterpriseWebLibrary.UserManagement.IdentityProviders;

/// <summary>
/// An identity provider that uses the system’s own user-management functionality.
/// </summary>
[ PublicAPI ]
public class LocalIdentityProvider: IdentityProvider {
	public delegate ( byte[]? salt, byte[]? hashedCode, Instant? expirationTime, byte? remainingAttemptCount, string destinationUrl ) LoginCodeGetterMethod(
		int userId );

	public delegate bool? PostAuthenticationMethod(
		SystemUser user, bool authenticationSuccessful, AuthenticationType authenticationType, out Action? unconditionalModificationMethod );

	public delegate void LoginCodeUpdaterMethod(
		int userId, byte[]? salt, byte[]? hashedCode, Instant? expirationTime, byte? remainingAttemptCount, string destinationUrl );

	internal delegate string AutoLogInPageUrlGetterMethod( string user, string code );

	internal delegate string ChangePasswordPageUrlGetterMethod( string destinationUrl );

	public enum AuthenticationType {
		Password,
		LoginCode
	}

	internal readonly string AdministratingOrganizationName;
	internal readonly string LogInHelpInstructions;
	private readonly PasswordStorageSetup passwordStorageSetup;
	private readonly LoginCodeGetterMethod loginCodeGetter;
	private readonly PostAuthenticationMethod? postAuthenticationMethod;
	internal readonly Duration? AuthenticationDuration;
	internal readonly Action<Validator, string>? PasswordValidationMethod;
	private readonly LoginCodeUpdaterMethod loginCodeUpdater;

	/// <summary>
	/// Creates a local identity provider.
	/// </summary>
	/// <param name="administratingOrganizationName">The name of the company/organization responsible for administrating the website. Do not pass null.</param>
	/// <param name="logInHelpInstructions">The text explaining what to do if the user has trouble logging in. An example is "call 555-555-5555." or "talk to
	/// XXX." Do not pass null.</param>
	/// <param name="passwordStorageSetup">The setup object for password storage.</param>
	/// <param name="loginCodeGetter">A function that takes a user ID and returns the corresponding user’s login-code data.</param>
	/// <param name="loginCodeUpdater">A method that takes a user ID and new login-code data and updates the corresponding user. You can also use this method to
	/// log that a login code has been sent. Do not pass null.</param>
	/// <param name="postAuthenticationMethod">Performs actions immediately after password or login-code authentication, which could include counting failed
	/// authentication attempts or preventing a user from logging in. Takes a user object and whether built-in authentication was successful, and returns true if
	/// authentication is successful, false if it failed for any reason, and null if it did not fail but is incomplete. Also has an out parameter for a
	/// modification method that will unconditionally execute. Do not use unless the system absolutely requires micromanagement of authentication behavior.
	/// </param>
	/// <param name="authenticationDuration">The duration of an authentication session. Pass null to use the default. Do not use unless the system absolutely
	/// requires micromanagement of authentication behavior.</param>
	/// <param name="passwordValidationMethod">Validates the specified password. Called when a user changes their password. Do not use unless the system
	/// absolutely requires micromanagement of authentication behavior.</param>
	public LocalIdentityProvider(
		string administratingOrganizationName, string logInHelpInstructions, PasswordStorageSetup passwordStorageSetup, LoginCodeGetterMethod loginCodeGetter,
		LoginCodeUpdaterMethod loginCodeUpdater, PostAuthenticationMethod? postAuthenticationMethod = null, Duration? authenticationDuration = null,
		Action<Validator, string>? passwordValidationMethod = null ) {
		AdministratingOrganizationName = administratingOrganizationName;
		LogInHelpInstructions = logInHelpInstructions;
		this.passwordStorageSetup = passwordStorageSetup;
		this.loginCodeGetter = loginCodeGetter;
		this.postAuthenticationMethod = postAuthenticationMethod;
		AuthenticationDuration = authenticationDuration;
		PasswordValidationMethod = passwordValidationMethod;
		this.loginCodeUpdater = loginCodeUpdater;
	}

	internal void UpdatePassword( int userId, string password ) {
		passwordStorageSetup.Updater( userId, password );
	}

	internal string? LogInUserWithPassword(
		string emailAddress, string password, out SystemUser? user, out Action? unconditionalModificationMethod, string errorMessage = "" ) {
		if( errorMessage.Length == 0 )
			errorMessage =
				"Login failed. Please check your email address and password. If you do not know your password, please set a new one using the button below.";

		user = null;
		unconditionalModificationMethod = null;

		if( passwordStorageSetup.Authenticator( emailAddress, password ) is not {} authenticationResult )
			return errorMessage;

		bool? authenticationSuccessful = authenticationResult.passwordCorrect;
		if( postAuthenticationMethod != null )
			authenticationSuccessful = postAuthenticationMethod(
				authenticationResult.user,
				authenticationResult.passwordCorrect,
				AuthenticationType.Password,
				out unconditionalModificationMethod );

		if( !authenticationResult.passwordCorrect || authenticationSuccessful == false )
			return errorMessage;
		user = authenticationResult.user;
		if( postAuthenticationMethod is not null )
			// Re-retrieve the user in case postAuthenticationMethod modified it.
			user = UserManagementStatics.SystemProvider.GetUser( user.UserId );
		return authenticationSuccessful == true ? "" : null;
	}

	/// <summary>
	/// Returns the user with the specified email address if the specified password is correct. Returns null if a user with that email address does not exist or
	/// if the password is incorrect.
	/// </summary>
	public SystemUser? AuthenticatePassword( string emailAddress, string password ) =>
		passwordStorageSetup.Authenticator( emailAddress, password ) is { passwordCorrect: true } result ? result.user : null;

	internal string SendLoginCode(
		string emailAddress, bool isPasswordReset, AutoLogInPageUrlGetterMethod autologInPageUrlGetter,
		ChangePasswordPageUrlGetterMethod changePasswordPageUrlGetter, string destinationUrl, int? newUserRoleId = null ) {
		var transactionTime = Clock.TransactionTime;

		var nextSendTimesByEmailAddress = AppMemoryCache.GetCacheValue(
			EwlStatics.EwlInitialism.ToLowerInvariant() + "LocalIdentityProviderCodeNextSendTimes",
			() => new ConcurrentDictionary<string, Instant>( StringComparer.Ordinal ) );

		var normalizedEmail = emailAddress.ToUpperInvariant();
		bool nextSendTimeExists;
		Instant nextSendTime;
		Instant newNextSendTime;
		do {
			nextSendTimeExists = nextSendTimesByEmailAddress.TryGetValue( normalizedEmail, out nextSendTime );
			if( nextSendTimeExists ) {
				var waitDuration = nextSendTime - transactionTime;
				if( waitDuration > Duration.Zero )
					return StringTools.ConcatenateWithDelimiter(
						" ",
						newUserRoleId.HasValue
							? $"A login code has already been sent to {emailAddress}."
							: $"A login code has already been sent to {emailAddress} if this address is registered with {AdministratingOrganizationName}.",
						$"Please wait {waitDuration.ToTimeSpan().Humanize( minUnit: Humanizer.Localisation.TimeUnit.Second )} before sending yourself another code." );
			}

			newNextSendTime = transactionTime + Duration.FromMinutes( 1 );
		}
		while( nextSendTimeExists
			       ? !nextSendTimesByEmailAddress.TryUpdate( normalizedEmail, newNextSendTime, nextSendTime )
			       : !nextSendTimesByEmailAddress.TryAdd( normalizedEmail, newNextSendTime ) );

		var user = UserManagementStatics.SystemProvider.GetUser( emailAddress );
		if( user is null ) {
			if( !newUserRoleId.HasValue )
				return "";
			user = UserManagementStatics.GetUser( UserManagementStatics.SystemProvider.InsertOrUpdateUser( null, emailAddress, newUserRoleId.Value ), true )!;
		}

		const string numbers = "123456789";
		var codeBuilder = new StringBuilder();
		for( var i = 0; i < 6; i += 1 )
			codeBuilder.Append( numbers[ RandomNumberGenerator.GetInt32( numbers.Length ) ] );
		var code = codeBuilder.ToString();
		var codeDuration = Duration.FromMinutes( 10 );

		var salt = RandomNumberGenerator.GetBytes( 16 );
		loginCodeUpdater(
			user.UserId,
			salt,
			getHashedLoginCode( code, salt ),
			transactionTime.Plus( codeDuration ),
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
			"<p>This login information is valid for {0}. If you need to log in later, please return to the page you were on and send yourself another login email.</p>"
				.FormatWith( codeDuration.ToTimeSpan().ToConciseString() ) );
		body.AppendLine( "<p>Thank you,<br>{0}</p>".FormatWith( AdministratingOrganizationName.Capitalize() ) );

		var message = new EmailMessage
			{
				Subject = isPasswordReset
					          ? "Reset password for {0}".FormatWith( SystemSpecificLogicStatics.SystemDisplayName )
					          : "Log in to {0}".FormatWith( SystemSpecificLogicStatics.SystemDisplayName ),
				BodyHtml = body.ToString()
			};
		message.ToAddresses.Add( new EmailAddress( emailAddress ) );
		EmailStatics.SendEmailWithDefaultFromAddress( message );

		return "";
	}

	internal string? LogInUserWithCode(
		string emailAddress, string code, out SystemUser? user, out string destinationUrl, out Action? unconditionalModificationMethod, string errorMessage = "" ) {
		if( errorMessage.Length == 0 )
			errorMessage = "The login code you entered is incorrect or has expired. Please check for typos. If there aren’t any, please send yourself another code.";

		user = null;
		destinationUrl = "";
		unconditionalModificationMethod = null;

		var userLocal = UserManagementStatics.SystemProvider.GetUser( emailAddress );
		if( userLocal == null )
			return errorMessage;

		var codeValid = true;

		var codeData = loginCodeGetter( userLocal.UserId );
		var unconditionalModMethods = new List<Action>();
		if( codeData.hashedCode == null )
			codeValid = false;
		else if( codeData.expirationTime!.Value <= Clock.TransactionTime )
			codeValid = false;
		else if( codeData.remainingAttemptCount!.Value == 0 )
			codeValid = false;
		else if( !codeData.hashedCode.SequenceEqual( getHashedLoginCode( code, codeData.salt! ) ) ) {
			codeValid = false;
			unconditionalModMethods.Add( () => loginCodeUpdater(
				userLocal.UserId,
				codeData.salt,
				codeData.hashedCode,
				codeData.expirationTime.Value,
				(byte)( codeData.remainingAttemptCount.Value - 1 ),
				codeData.destinationUrl ) );
		}

		bool? authenticationSuccessful = codeValid;
		if( postAuthenticationMethod != null ) {
			authenticationSuccessful = postAuthenticationMethod( userLocal, codeValid, AuthenticationType.LoginCode, out var unconditionalModMethod );
			if( unconditionalModMethod is not null )
				unconditionalModMethods.Add( unconditionalModMethod );

			// Re-retrieve the user in case postAuthenticationMethod modified it.
			userLocal = UserManagementStatics.SystemProvider.GetUser( userLocal.UserId )!;
		}

		if( unconditionalModMethods.Any() )
			unconditionalModificationMethod = () => {
				foreach( var i in unconditionalModMethods )
					i();
			};

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

/// <summary>
/// A configuration for password storage.
/// </summary>
[ PublicAPI ]
public class PasswordStorageSetup {
	public delegate ( SystemUser user, bool passwordCorrect )? AuthenticatorMethod( string emailAddress, string password );

	public delegate void HashingUpdaterMethod( int userId, string password );

	public delegate void UpdaterMethod( int userId, int salt, byte[] saltedPassword );

	/// <summary>
	/// Creates a setup object for standard password storage.
	/// </summary>
	/// <param name="getter">A function that takes an email address and returns the corresponding user object along with the user’s salt and salted password, or
	/// null if a user with that email address does not exist. Do not pass null. We recommend that you use case-insensitive comparison.</param>
	/// <param name="updater">A method that takes a user ID and new password data and updates the corresponding user. Do not pass null.</param>
	public static PasswordStorageSetup CreateStandard( Func<string, ( SystemUser user, int salt, byte[]? saltedPassword )?> getter, UpdaterMethod updater ) =>
		new(
			( emailAddress, password ) => {
				if( getter( emailAddress ) is not {} userData )
					return null;
				var passwordCorrect = userData.saltedPassword?.SequenceEqual( getHashedPassword( password, userData.salt ) ) == true;
				return ( userData.user, passwordCorrect );
			},
			( userId, password ) => {
				var salt = BitConverter.ToInt32( RandomNumberGenerator.GetBytes( 4 ), 0 );
				updater( userId, salt, getHashedPassword( password, salt ) );
			} );

	private static byte[] getHashedPassword( string password, int salt ) {
		// Code from http://www.aspheute.com/english/20040105.asp.

		// Create a new salt
		var saltBytes = new byte[ 4 ];
		unchecked {
			saltBytes[ 0 ] = (byte)( salt >> 24 );
			saltBytes[ 1 ] = (byte)( salt >> 16 );
			saltBytes[ 2 ] = (byte)( salt >> 8 );
			saltBytes[ 3 ] = (byte)( salt );
		}

		// Create Byte array of password string
		var encoder = new ASCIIEncoding();
		var secretBytes = encoder.GetBytes( password );

		// append the two arrays
		var toHash = new byte[ secretBytes.Length + saltBytes.Length ];
		Array.Copy( secretBytes, 0, toHash, 0, secretBytes.Length );
		Array.Copy( saltBytes, 0, toHash, secretBytes.Length, saltBytes.Length );

		var sha1 = SHA1.Create();
		return sha1.ComputeHash( toHash );
	}

	/// <summary>
	/// Creates a setup object for custom password storage. Do not use unless the system absolutely requires micromanagement of authentication behavior.
	/// </summary>
	public static PasswordStorageSetup CreateCustom( AuthenticatorMethod authenticator, HashingUpdaterMethod updater ) => new( authenticator, updater );

	internal readonly AuthenticatorMethod Authenticator;
	internal readonly HashingUpdaterMethod Updater;

	private PasswordStorageSetup( AuthenticatorMethod authenticator, HashingUpdaterMethod updater ) {
		Authenticator = authenticator;
		Updater = updater;
	}
}