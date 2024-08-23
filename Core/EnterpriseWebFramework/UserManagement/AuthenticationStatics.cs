#nullable disable
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Principal;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.UserManagement;
using EnterpriseWebLibrary.UserManagement.IdentityProviders;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using NodaTime;
using NodaTime.Text;
using Tewl.InputValidation;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;

[ PublicAPI ]
public static class AuthenticationStatics {
	private const string userCookieName = "User";
	private const string identityProviderCookieName = "IdentityProvider";
	private const string testCookieName = "TestCookie";

	private static readonly Duration defaultAuthenticationDuration = Duration.FromHours( 32 ); // persist across consecutive days of usage

	private static AppAuthenticationProvider provider;
	private static TicketDataFormat authenticationTicketProtector;
	private static Func<string, ResourceBase> defaultLogInPageGetter;
	private static LocalIdentityProvider.AutoLogInPageUrlGetterMethod autoLogInPageUrlGetter;
	private static LocalIdentityProvider.ChangePasswordPageUrlGetterMethod changePasswordPageUrlGetter;

	private static IReadOnlyCollection<SamlIdentityProvider> samlIdentityProviders;

	public delegate SystemUser PasswordLoginModificationMethod( DataValue<string> emailAddress, DataValue<string> password, string errorMessage = "" );

	public delegate void LoginCodeSenderMethod( DataValue<string> emailAddress, bool isPasswordReset, string destinationUrl, int? newUserRoleId = null );

	public delegate ( SystemUser user, string destinationUrl ) CodeLoginModificationMethod( string emailAddress, string code, string errorMessage = "" );

	/// <summary>
	/// Logs in the user with the specified ID.
	/// </summary>
	/// <param name="userId">The user ID.</param>
	/// <param name="authenticationDuration">The duration of the authentication session. Pass null to use the default. Do not use unless the system absolutely
	/// requires micromanagement of authentication behavior.</param>
	public delegate void SpecifiedUserLoginModificationMethod( int userId, Duration? authenticationDuration = null );

	internal static void Init(
		SystemProviderReference<AppAuthenticationProvider> provider, IDataProtectionProvider dataProtectionProvider,
		Func<string, ResourceBase> defaultLogInPageGetter, LocalIdentityProvider.AutoLogInPageUrlGetterMethod autoLogInPageUrlGetter,
		LocalIdentityProvider.ChangePasswordPageUrlGetterMethod changePasswordPageUrlGetter ) {
		AuthenticationStatics.provider = provider.GetProvider( returnNullIfNotFound: true ) ?? new AppAuthenticationProvider();
		authenticationTicketProtector = new TicketDataFormat( dataProtectionProvider.CreateProtector( "EnterpriseWebLibrary.WebFramework.UserManagement" ) );
		AuthenticationStatics.defaultLogInPageGetter = defaultLogInPageGetter;
		AuthenticationStatics.autoLogInPageUrlGetter = autoLogInPageUrlGetter;
		AuthenticationStatics.changePasswordPageUrlGetter = changePasswordPageUrlGetter;
	}

	internal static AppAuthenticationProvider AppProvider => provider;

	internal static void InitAppSpecificLogicDependencies() {
		// In the future we expect this to use logic from AppAuthenticationProvider to potentially filter the system’s identity providers.
		samlIdentityProviders =
			( UserManagementStatics.UserManagementEnabled
				  ? UserManagementStatics.IdentityProviders.OfType<SamlIdentityProvider>()
				  : Enumerable.Empty<SamlIdentityProvider>() ).Materialize();
	}

	internal static IReadOnlyCollection<SamlIdentityProvider> SamlIdentityProviders => samlIdentityProviders;

	/// <summary>
	/// Returns the default log-in page for the application. This is useful if you need a direct hyperlink to it.
	/// </summary>
	public static ResourceBase GetDefaultLogInPage( string returnUrl ) => defaultLogInPageGetter( returnUrl );


	/// <summary>
	/// The second item in the returned tuple will be (1) null if impersonation is not taking place, (2) a value with a null user if impersonation is taking place
	/// with an impersonator who doesn’t correspond to a user, or (3) a value containing the impersonator.
	/// </summary>
	internal static Tuple<SystemUser, SpecifiedValue<SystemUser>> GetUserAndImpersonatorFromCookies() {
		if( !UserManagementStatics.UserManagementEnabled )
			return Tuple.Create<SystemUser, SpecifiedValue<SystemUser>>( null, null );

		SystemUser getUser() {
			if( !CookieStatics.TryGetCookieValueFromResponseOrRequest( userCookieName, out var cookieValue ) || cookieValue is null )
				return null;
			var ticket = GetFormsAuthTicket( cookieValue );
			return ticket != null ? UserManagementStatics.GetUser( int.Parse( ticket.Principal.Identity.Name ), false ) : null;
		}
		var user = getUser();

		if( UserCanImpersonate( user ) )
			if( CookieStatics.TryGetCookieValueFromResponseOrRequest( UserImpersonationStatics.CookieName, out var cookieValue ) && cookieValue is not null )
				return Tuple.Create(
					cookieValue.Length > 0 ? UserManagementStatics.GetUser( int.Parse( cookieValue ), false ) : null,
					new SpecifiedValue<SystemUser>( user ) );

		return Tuple.Create( user, (SpecifiedValue<SystemUser>)null );
	}

	internal static bool UserCanImpersonate( SystemUser user ) => user is { Role.CanManageUsers: true } || !ConfigurationStatics.IsLiveInstallation;


	// Adding a New User

	/// <summary>
	/// Gets password and "password again" form items. The validation ensures that the two form items contain identical, valid passwords.
	/// </summary>
	/// <param name="passwordUpdater">A method that takes a user ID and updates the password data for the corresponding user. Do not pass null.</param>
	/// <param name="firstLabel"></param>
	/// <param name="secondLabel"></param>
	public static IReadOnlyCollection<FormItem> GetPasswordModificationFormItems(
		out Action<int> passwordUpdater, IEnumerable<PhrasingComponent> firstLabel = null, IEnumerable<PhrasingComponent> secondLabel = null ) {
		var password = new DataValue<string>();
		var passwordAgainFormItem = password.ToTextControl( true, setup: TextControlSetup.CreateObscured( autoFillTokens: "new-password" ), value: "" )
			.ToFormItem( label: secondLabel?.Materialize() ?? "Password again".ToComponents() );

		var passwordFormItem = new TextControl(
			"",
			true,
			setup: TextControlSetup.CreateObscured( autoFillTokens: "new-password" ),
			validationMethod: ( postBackValue, validator ) => {
				if( postBackValue != password.Value )
					validator.NoteErrorAndAddMessage( "Passwords do not match." );
				else {
					if( UserManagementStatics.LocalIdentityProvider.PasswordValidationMethod != null )
						UserManagementStatics.LocalIdentityProvider.PasswordValidationMethod( validator, password.Value );
					else if( password.Value.Length < 7 )
						validator.NoteErrorAndAddMessage( "Passwords must be at least 7 characters long." );
				}
			} ).ToFormItem( label: firstLabel?.Materialize() ?? "Password".ToComponents() );

		passwordUpdater = userId => {
			if( !password.Changed )
				return;
			var p = new LocalIdentityProvider.Password( password.Value );
			UserManagementStatics.LocalIdentityProvider.PasswordUpdater( userId, p.Salt, p.ComputeSaltedHash() );
		};

		return new[] { passwordFormItem, passwordAgainFormItem };
	}


	// Log-In

	/// <summary>
	/// Gets an email address form item for use on log-in pages.
	/// </summary>
	public static FormItem GetEmailAddressFormItem(
		this DataValue<string> emailAddress, IReadOnlyCollection<PhrasingComponent> label, Action<Validator> additionalValidationMethod = null ) =>
		// The username token probably works better for password managers; see https://stackoverflow.com/a/57902690/35349.
		emailAddress.ToEmailAddressControl(
				false,
				setup: EmailAddressControlSetup.Create( autoFillTokens: "username" ),
				value: "",
				additionalValidationMethod: additionalValidationMethod )
			.ToFormItem( label: label );

	/// <summary>
	/// Gets a login code form item for use on log-in pages.
	/// </summary>
	public static FormItem GetLoginCodeFormItem( this DataValue<string> loginCode ) =>
		loginCode.ToNumericTextControl( false, value: "", maxLength: 10 ).ToFormItem( label: "Login code".ToComponents() );

	/// <summary>
	/// Returns log-in hidden fields and modification methods for logging in a user. Also sets up client-side logic for user log-in. Do not call if user
	/// management is not enabled, and use only the specified-user login method if the local identity provider is not enabled.
	/// </summary>
	public static ( IReadOnlyCollection<EtherealComponent> hiddenFields, ( PasswordLoginModificationMethod passwordLoginMethod, LoginCodeSenderMethod
		loginCodeSender, CodeLoginModificationMethod codeLoginMethod, SpecifiedUserLoginModificationMethod specifiedUserLoginMethod ) modificationMethods )
		GetLogInHiddenFieldsAndMethods() {
		ResourceBase.ExecuteDataModificationMethod( SetTestCookie );
		var clientTime = new DataValue<string>();
		var hiddenFields = GetLogInHiddenFields( clientTime );

		return ( hiddenFields, ( ( emailAddress, password, errorMessage ) => {
				                       var errors = new List<string>();

				                       errorMessage = UserManagementStatics.LocalIdentityProvider.LogInUserWithPassword(
					                       emailAddress.Value,
					                       password.Value,
					                       out var user,
					                       out var unconditionalModMethod,
					                       errorMessage: errorMessage );
				                       if( errorMessage == null )
					                       LogOutUser();
				                       else if( errorMessage.Any() )
					                       errors.Add( errorMessage );
				                       else
					                       SetFormsAuthCookieAndUser( user, identityProvider: UserManagementStatics.LocalIdentityProvider );

				                       errors.AddRange( verifyTestCookie() );
				                       addStatusMessageIfClockNotSynchronized( clientTime );

				                       if( errors.Any() )
					                       throw new DataModificationException( errors.ToArray(), modificationMethod: unconditionalModMethod );

				                       if( unconditionalModMethod is not null ) {
					                       unconditionalModMethod();

					                       // Re-retrieve the user in case unconditionalModMethod modified it.
					                       user = UserManagementStatics.SystemProvider.GetUser( user.UserId );
				                       }

				                       return user;
			                       }, ( emailAddress, isPasswordReset, destinationUrl, newUserRoleId ) => {
				                       UserManagementStatics.LocalIdentityProvider.SendLoginCode(
					                       emailAddress.Value,
					                       isPasswordReset,
					                       autoLogInPageUrlGetter,
					                       changePasswordPageUrlGetter,
					                       destinationUrl,
					                       newUserRoleId: newUserRoleId );
				                       PageBase.AddStatusMessage( StatusMessageType.Info, "Your login code has been sent to {0}.".FormatWith( emailAddress.Value ) );
			                       }, ( emailAddress, code, errorMessage ) => {
				                       var errors = new List<string>();

				                       errorMessage = UserManagementStatics.LocalIdentityProvider.LogInUserWithCode(
					                       emailAddress,
					                       code,
					                       out var user,
					                       out var destinationUrl,
					                       out var unconditionalModMethod,
					                       errorMessage: errorMessage );
				                       if( errorMessage == null )
					                       LogOutUser();
				                       else if( errorMessage.Any() )
					                       errors.Add( errorMessage );
				                       else
					                       SetFormsAuthCookieAndUser( user, identityProvider: UserManagementStatics.LocalIdentityProvider );

				                       errors.AddRange( verifyTestCookie() );
				                       addStatusMessageIfClockNotSynchronized( clientTime );

				                       if( errors.Any() )
					                       throw new DataModificationException( errors.ToArray(), modificationMethod: unconditionalModMethod );

				                       if( unconditionalModMethod is not null ) {
					                       unconditionalModMethod();

					                       // Re-retrieve the user in case unconditionalModMethod modified it.
					                       user = UserManagementStatics.SystemProvider.GetUser( user.UserId );
				                       }

				                       return ( user, destinationUrl );
			                       }, ( userId, authenticationDuration ) => {
				                       var user = UserManagementStatics.SystemProvider.GetUser( userId );
				                       SetFormsAuthCookieAndUser( user, authenticationDuration: authenticationDuration );

				                       var errors = new List<string>();
				                       errors.AddRange( verifyTestCookie() );
				                       addStatusMessageIfClockNotSynchronized( clientTime );
				                       if( errors.Any() )
					                       throw new DataModificationException( errors.ToArray() );
			                       } ) );
	}

	/// <summary>
	/// Sets a test cookie that is verified during user log-in. Only necessary when calling a log-in modification method from a page-load post-back, in which case
	/// the log-in page may not be able to set the cookie itself in time for verification.
	/// </summary>
	public static void SetTestCookie() {
		setCookie( testCookieName, "No data" );
	}

	internal static IReadOnlyCollection<EtherealComponent> GetLogInHiddenFields( DataValue<string> clientTime ) {
		var timeHiddenFieldId = new HiddenFieldId();
		return new EwfHiddenField(
			"",
			id: timeHiddenFieldId,
			validationMethod: ( postBackValue, _ ) => clientTime.Value = postBackValue.Value,
			jsInitStatementGetter: id => "$( document.getElementById( '{0}' ).form ).submit( function() {{ {1} }} );".FormatWith(
				id,
				timeHiddenFieldId.GetJsValueModificationStatements( "new Date().toISOString()" ) ) ).PageComponent.ToCollection();
	}

	/// <summary>
	/// MVC and private use only.
	/// </summary>
	public static void SetFormsAuthCookieAndUser( SystemUser user, IdentityProvider identityProvider = null, Duration? authenticationDuration = null ) {
		if( RequestState.Instance.ImpersonatorExists )
			UserImpersonationStatics.SetCookie( user );
		else {
			authenticationDuration ??= identityProvider is LocalIdentityProvider { AuthenticationDuration: not null } local ? local.AuthenticationDuration.Value :
			                           identityProvider is SamlIdentityProvider { AuthenticationDuration: not null } saml ? saml.AuthenticationDuration.Value :
			                           user.Role.RequiresEnhancedSecurity ? Duration.FromMinutes( 12 ) : defaultAuthenticationDuration;

			var ticket = new AuthenticationTicket(
				new ClaimsPrincipal( new GenericIdentity( user.UserId.ToString() ) ),
				new AuthenticationProperties
					{
						IssuedUtc = EwfRequest.Current.RequestTime.ToDateTimeOffset(),
						ExpiresUtc = EwfRequest.Current.RequestTime.Plus( authenticationDuration.Value ).ToDateTimeOffset()
					},
				EwlStatics.EwlInitialism );
			setFormsAuthCookie( ticket );
		}

		if( identityProvider != null )
			SetUserLastIdentityProvider( identityProvider );
		else
			CookieStatics.ClearCookie( identityProviderCookieName );

		ResourceBase.RefreshRequestState();
	}

	private static void setFormsAuthCookie( AuthenticationTicket ticket ) {
		setCookie( userCookieName, authenticationTicketProtector.Protect( ticket ) );
	}

	private static void setCookie( string name, string value ) {
		CookieStatics.SetCookie( name, value, null, EwfConfigurationStatics.AppSupportsSecureConnections, true );
	}

	private static IEnumerable<string> verifyTestCookie() =>
		TestCookieMissing() ? Translation.YourBrowserHasCookiesDisabled.ToCollection() : Enumerable.Empty<string>();

	private static void addStatusMessageIfClockNotSynchronized( DataValue<string> clientTime ) {
		if( ClockNotSynchronized( clientTime ) )
			PageBase.AddStatusMessage( StatusMessageType.Warning, GetClockWrongMessage() );
	}


	// Cookie Updating

	internal static Action GetUserCookieUpdater() {
		if( !CookieStatics.TryGetCookieValueFromRequestOnly( userCookieName, out var cookieValue ) )
			return null;

		var prefixedCookieName = ( EwfConfigurationStatics.AppConfiguration.DefaultCookieAttributes.NamePrefix ?? "" ) + userCookieName;
		if( CookieStatics.ResponseCookies.Any( i => string.Equals( i.name, prefixedCookieName, StringComparison.Ordinal ) ) )
			return null;

		var ticket = GetFormsAuthTicket( cookieValue );
		if( ticket is null )
			return clearFormsAuthCookie;

		var passedDuration = EwfRequest.Current.RequestTime - Instant.FromDateTimeOffset( ticket.Properties.IssuedUtc.Value );
		var totalDuration = Duration.FromTimeSpan( ticket.Properties.ExpiresUtc.Value - ticket.Properties.IssuedUtc.Value );
		if( passedDuration / totalDuration < .5 )
			return null;

		ticket.Properties.IssuedUtc = ticket.Properties.IssuedUtc.Value + passedDuration.ToTimeSpan();
		ticket.Properties.ExpiresUtc = ticket.Properties.ExpiresUtc.Value + passedDuration.ToTimeSpan();
		return () => setFormsAuthCookie( ticket );
	}

	internal static AuthenticationTicket GetFormsAuthTicket( string cookie ) {
		AuthenticationTicket ticket = null;
		try {
			ticket = authenticationTicketProtector.Unprotect( cookie );
		}
		catch( CryptographicException ) {}
		return ticket != null && EwfRequest.Current.RequestTime < Instant.FromDateTimeOffset( ticket.Properties.ExpiresUtc.Value ) ? ticket : null;
	}


	// Log-Out

	/// <summary>
	/// Do not call if the system does not implement the forms authentication capable user management provider.
	/// </summary>
	public static void LogOutUser() {
		if( RequestState.Instance.ImpersonatorExists )
			UserImpersonationStatics.SetCookie( null );
		else
			clearFormsAuthCookie();

		CookieStatics.ClearCookie( identityProviderCookieName );

		ResourceBase.RefreshRequestState();
	}

	private static void clearFormsAuthCookie() {
		CookieStatics.ClearCookie( userCookieName );
	}


	// User’s last identity provider

	internal static IdentityProvider GetUserLastIdentityProvider() =>
		// Ignore the cookie if the existence of a user has changed since that could mean the user timed out.
		CookieStatics.TryGetCookieValueFromResponseOrRequest( identityProviderCookieName, out var cookieValue ) && cookieValue is not null &&
		cookieValue[ 0 ] == ( SystemUser.Current is not null ? '+' : '-' )
			? UserManagementStatics.IdentityProviders.SingleOrDefault(
				identityProvider => string.Equals(
					identityProvider is LocalIdentityProvider ? "Local" :
					identityProvider is SamlIdentityProvider saml ? saml.EntityId : throw new ApplicationException( "identity provider" ),
					cookieValue.Substring( 1 ),
					StringComparison.Ordinal ) )
			: null;

	internal static void SetUserLastIdentityProvider( IdentityProvider identityProvider ) {
		setCookie(
			identityProviderCookieName,
			( SystemUser.Current is not null ? "+" : "-" ) + ( identityProvider is LocalIdentityProvider ? "Local" :
			                                                   identityProvider is SamlIdentityProvider saml ? saml.EntityId :
			                                                   throw new ApplicationException( "identity provider" ) ) );
	}


	// Client-side functionality verification

	internal static bool TestCookieMissing() => !CookieStatics.TryGetCookieValueFromRequestOnly( testCookieName, out _ );

	internal static bool ClockNotSynchronized( DataValue<string> clientTime ) {
		var clientParseResult = InstantPattern.ExtendedIso.Parse( clientTime.Value );
		if( !clientParseResult.Success )
			throw new DataModificationException( "Your browser did not submit the current time." );

		var clockDifference = clientParseResult.GetValueOrThrow() - EwfRequest.Current.RequestTime;
		return Math.Abs( clockDifference.TotalMinutes ) > 5;
	}

	internal static string GetClockWrongMessage() {
		var timeZone = DateTimeZoneProviders.Tzdb.GetSystemDefault();
		return Translation.YourClockIsWrong + " " + EwfRequest.Current.RequestTime.InZone( timeZone ).ToDateTimeUnspecified().ToHourAndMinuteString() + " " +
		       timeZone.GetZoneInterval( EwfRequest.Current.RequestTime ).Name + ".";
	}
}