using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Principal;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.UserManagement;
using EnterpriseWebLibrary.UserManagement.IdentityProviders;
using EnterpriseWebLibrary.WebSessionState;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using NodaTime;
using NodaTime.Text;
using Tewl.InputValidation;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement {
	public static class AuthenticationStatics {
		private const string userCookieName = "User";
		private const string identityProviderCookieName = "IdentityProvider";
		private const string testCookieName = "TestCookie";

		/// <summary>
		/// The idle time required for a session to be erased.
		/// </summary>
		public static readonly Duration SessionDuration = Duration.FromHours( 32 ); // persist across consecutive days of usage

		private static AppAuthenticationProvider provider;
		private static TicketDataFormat authenticationTicketProtector;
		private static LocalIdentityProvider.AutoLogInPageUrlGetterMethod autoLogInPageUrlGetter;
		private static LocalIdentityProvider.ChangePasswordPageUrlGetterMethod changePasswordPageUrlGetter;

		private static IReadOnlyCollection<SamlIdentityProvider> samlIdentityProviders;

		public delegate User PasswordLoginModificationMethod( DataValue<string> emailAddress, DataValue<string> password, string errorMessage = "" );

		public delegate void LoginCodeSenderMethod( DataValue<string> emailAddress, bool isPasswordReset, string destinationUrl, int? newUserRoleId = null );

		public delegate ( User user, string destinationUrl ) CodeLoginModificationMethod( string emailAddress, string code, string errorMessage = "" );

		public delegate void SpecifiedUserLoginModificationMethod( int userId );

		internal static void Init(
			SystemProviderReference<AppAuthenticationProvider> provider, IDataProtectionProvider dataProtectionProvider,
			LocalIdentityProvider.AutoLogInPageUrlGetterMethod autoLogInPageUrlGetter,
			LocalIdentityProvider.ChangePasswordPageUrlGetterMethod changePasswordPageUrlGetter ) {
			AuthenticationStatics.provider = provider.GetProvider( returnNullIfNotFound: true ) ?? new AppAuthenticationProvider();
			authenticationTicketProtector = new TicketDataFormat( dataProtectionProvider.CreateProtector( "EnterpriseWebLibrary.WebFramework.UserManagement" ) );
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
		/// The second item in the returned tuple will be (1) null if impersonation is not taking place, (2) a value with a null user if impersonation is taking
		/// place with an impersonator who doesn't correspond to a user, or (3) a value containing the impersonator.
		/// </summary>
		internal static Tuple<User, SpecifiedValue<User>> GetUserAndImpersonatorFromRequest() {
			if( !UserManagementStatics.UserManagementEnabled )
				return Tuple.Create<User, SpecifiedValue<User>>( null, null );

			User getUser() {
				if( !CookieStatics.TryGetCookieValue( userCookieName, out var cookieValue ) )
					return null;
				var ticket = GetFormsAuthTicket( cookieValue );
				return ticket != null ? UserManagementStatics.GetUser( int.Parse( ticket.Principal.Identity.Name ), false ) : null;
			}
			var user = getUser();

			if( UserCanImpersonate( user ) )
				if( CookieStatics.TryGetCookieValue( UserImpersonationStatics.CookieName, out var cookieValue ) )
					return Tuple.Create( cookieValue.Any() ? UserManagementStatics.GetUser( int.Parse( cookieValue ), false ) : null, new SpecifiedValue<User>( user ) );

			return Tuple.Create( user, (SpecifiedValue<User>)null );
		}

		internal static Tuple<User, SpecifiedValue<User>> RefreshUserAndImpersonator( Tuple<User, SpecifiedValue<User>> userAndImpersonator ) {
			SpecifiedValue<User> impersonator;
			if( userAndImpersonator.Item2 == null )
				impersonator = null;
			else {
				var impersonatorUser = userAndImpersonator.Item2.Value != null ? UserManagementStatics.GetUser( userAndImpersonator.Item2.Value.UserId, false ) : null;
				impersonator = UserCanImpersonate( impersonatorUser ) ? new SpecifiedValue<User>( impersonatorUser ) : null;
			}

			return Tuple.Create( userAndImpersonator.Item1 != null ? UserManagementStatics.GetUser( userAndImpersonator.Item1.UserId, false ) : null, impersonator );
		}

		internal static bool UserCanImpersonate( User user ) => ( user != null && user.Role.CanManageUsers ) || !ConfigurationStatics.IsLiveInstallation;


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
			var clientTime = new DataValue<string>();
			var hiddenFields = GetLogInHiddenFieldsAndSetUpClientSideLogic( clientTime );

			return ( hiddenFields, ( ( emailAddress, password, errorMessage ) => {
					                       var errors = new List<string>();

					                       errorMessage = UserManagementStatics.LocalIdentityProvider.LogInUserWithPassword(
						                       emailAddress.Value,
						                       password.Value,
						                       out var user,
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
						                       throw new DataModificationException( errors.ToArray() );
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
						                       throw new DataModificationException( errors.ToArray() );
					                       return ( user, destinationUrl );
				                       }, userId => {
					                       var user = UserManagementStatics.SystemProvider.GetUser( userId );
					                       SetFormsAuthCookieAndUser( user );

					                       var errors = new List<string>();
					                       errors.AddRange( verifyTestCookie() );
					                       addStatusMessageIfClockNotSynchronized( clientTime );
					                       if( errors.Any() )
						                       throw new DataModificationException( errors.ToArray() );
				                       } ) );
		}

		internal static IReadOnlyCollection<EtherealComponent> GetLogInHiddenFieldsAndSetUpClientSideLogic( DataValue<string> clientTime ) {
			setCookie( testCookieName, "No data" );

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
		public static void SetFormsAuthCookieAndUser( User user, IdentityProvider identityProvider = null ) {
			if( AppRequestState.Instance.ImpersonatorExists )
				UserImpersonationStatics.SetCookie( user );
			else {
				// If the user's role requires enhanced security, require re-authentication every 12 minutes. Otherwise, make it the same as a session timeout.
				var authenticationDuration = identityProvider is LocalIdentityProvider local && local.AuthenticationTimeoutMinutes.HasValue
					                             ?
					                             Duration.FromMinutes( local.AuthenticationTimeoutMinutes.Value )
					                             : user.Role.RequiresEnhancedSecurity
						                             ? Duration.FromMinutes( 12 )
						                             : SessionDuration;

				var ticket = new AuthenticationTicket(
					new ClaimsPrincipal( new GenericIdentity( user.UserId.ToString() ) ),
					new AuthenticationProperties
						{
							IssuedUtc = AppRequestState.RequestTime.ToDateTimeOffset(),
							ExpiresUtc = AppRequestState.RequestTime.Plus( authenticationDuration ).ToDateTimeOffset()
						},
					EwlStatics.EwlInitialism );
				AppRequestState.AddNonTransactionalModificationMethod( () => setFormsAuthCookie( ticket ) );
			}
			AppRequestState.Instance.SetUser( user );

			if( identityProvider != null )
				AppRequestState.AddNonTransactionalModificationMethod( () => SetUserLastIdentityProvider( identityProvider ) );
			else
				AppRequestState.AddNonTransactionalModificationMethod( () => CookieStatics.ClearCookie( identityProviderCookieName ) );
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

		internal static void UpdateFormsAuthCookieIfNecessary() {
			if( !CookieStatics.TryGetCookieValue( userCookieName, out var cookieValue ) )
				return;

			var ticket = GetFormsAuthTicket( cookieValue );
			if( ticket != null ) {
				var passedDuration = AppRequestState.RequestTime - Instant.FromDateTimeOffset( ticket.Properties.IssuedUtc.Value );
				var totalDuration = Duration.FromTimeSpan( ticket.Properties.ExpiresUtc.Value - ticket.Properties.IssuedUtc.Value );
				if( passedDuration / totalDuration >= .5 ) {
					ticket.Properties.IssuedUtc = ticket.Properties.IssuedUtc.Value + passedDuration.ToTimeSpan();
					ticket.Properties.ExpiresUtc = ticket.Properties.ExpiresUtc.Value + passedDuration.ToTimeSpan();
					setFormsAuthCookie( ticket );
				}
			}
			else
				clearFormsAuthCookie();
		}

		internal static AuthenticationTicket GetFormsAuthTicket( string cookie ) {
			AuthenticationTicket ticket = null;
			try {
				ticket = authenticationTicketProtector.Unprotect( cookie );
			}
			catch( CryptographicException ) {}
			return ticket != null && AppRequestState.RequestTime < Instant.FromDateTimeOffset( ticket.Properties.ExpiresUtc.Value ) ? ticket : null;
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

			AppRequestState.AddNonTransactionalModificationMethod( () => CookieStatics.ClearCookie( identityProviderCookieName ) );
		}

		private static void clearFormsAuthCookie() {
			CookieStatics.ClearCookie( userCookieName );
		}


		// User’s last identity provider

		internal static IdentityProvider GetUserLastIdentityProvider() =>
			// Ignore the cookie if the existence of a user has changed since that could mean the user timed out.
			CookieStatics.TryGetCookieValue( identityProviderCookieName, out var cookieValue ) && cookieValue[ 0 ] == ( AppTools.User != null ? '+' : '-' )
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
				( AppTools.User != null ? "+" : "-" ) + ( identityProvider is LocalIdentityProvider ? "Local" :
				                                          identityProvider is SamlIdentityProvider saml ? saml.EntityId :
				                                          throw new ApplicationException( "identity provider" ) ) );
		}


		// Client-side functionality verification

		internal static bool TestCookieMissing() => !CookieStatics.TryGetCookieValue( testCookieName, out _ );

		internal static bool ClockNotSynchronized( DataValue<string> clientTime ) {
			var clientParseResult = InstantPattern.ExtendedIso.Parse( clientTime.Value );
			if( !clientParseResult.Success )
				throw new DataModificationException( "Your browser did not submit the current time." );

			var clockDifference = clientParseResult.GetValueOrThrow() - AppRequestState.RequestTime;
			return Math.Abs( clockDifference.TotalMinutes ) > 5;
		}

		internal static string GetClockWrongMessage() {
			var timeZone = DateTimeZoneProviders.Tzdb.GetSystemDefault();
			return Translation.YourClockIsWrong + " " + AppRequestState.RequestTime.InZone( timeZone ).ToDateTimeUnspecified().ToHourAndMinuteString() + " " +
			       timeZone.GetZoneInterval( AppRequestState.RequestTime ).Name + ".";
		}
	}
}