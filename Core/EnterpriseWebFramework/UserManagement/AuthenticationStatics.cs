using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.UserManagement;
using EnterpriseWebLibrary.UserManagement.IdentityProviders;
using EnterpriseWebLibrary.WebSessionState;
using Humanizer;
using NodaTime;
using NodaTime.Text;
using Tewl.InputValidation;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement {
	public static class AuthenticationStatics {
		private const string userCookieName = "User";
		private const string identityProviderCookieName = "IdentityProvider";
		private const string testCookieName = "TestCookie";

		/// <summary>
		/// The idle time required for a session to be erased.
		/// </summary>
		public static readonly TimeSpan SessionDuration = TimeSpan.FromHours( 32 ); // persist across consecutive days of usage

		private static SystemProviderReference<AppAuthenticationProvider> provider;
		private static LocalIdentityProvider.AutoLogInPageUrlGetterMethod autoLogInPageUrlGetter;
		private static LocalIdentityProvider.ChangePasswordPageUrlGetterMethod changePasswordPageUrlGetter;

		private static IReadOnlyCollection<SamlIdentityProvider> samlIdentityProviders;

		public delegate User PasswordLoginModificationMethod( DataValue<string> emailAddress, DataValue<string> password, string errorMessage = "" );

		public delegate void LoginCodeSenderMethod( DataValue<string> emailAddress, bool isPasswordReset, string destinationUrl, int? newUserRoleId = null );

		public delegate ( User user, string destinationUrl ) CodeLoginModificationMethod( string emailAddress, string code, string errorMessage = "" );

		public delegate void SpecifiedUserLoginModificationMethod( int userId );

		internal static void Init(
			SystemProviderReference<AppAuthenticationProvider> provider, LocalIdentityProvider.AutoLogInPageUrlGetterMethod autoLogInPageUrlGetter,
			LocalIdentityProvider.ChangePasswordPageUrlGetterMethod changePasswordPageUrlGetter ) {
			AuthenticationStatics.provider = provider;
			AuthenticationStatics.autoLogInPageUrlGetter = autoLogInPageUrlGetter;
			AuthenticationStatics.changePasswordPageUrlGetter = changePasswordPageUrlGetter;
		}

		internal static AppAuthenticationProvider AppProvider => provider.GetProvider();

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
			var userLazy = new Func<User>[]
					{
						() => {
							var cookie = CookieStatics.GetCookie( userCookieName );
							if( cookie == null )
								return null;
							var ticket = GetFormsAuthTicket( cookie );
							return ticket != null ? UserManagementStatics.GetUser( int.Parse( ticket.Name ), false ) : null;
						},
						() => {
							var identity = HttpContext.Current.User.Identity;
							return identity.IsAuthenticated && identity.AuthenticationType == CertificateAuthenticationModule.CertificateAuthenticationType
								       ? UserManagementStatics.SystemProvider.GetUser( identity.Name )
								       : null;
						}
					}.Select( i => new Lazy<User>( i ) )
				.FirstOrDefault( i => i.Value != null );
			var user = userLazy != null ? userLazy.Value : null;

			if( ( user != null && user.Role.CanManageUsers ) || !ConfigurationStatics.IsLiveInstallation ) {
				var cookie = CookieStatics.GetCookie( UserImpersonationStatics.CookieName );
				if( cookie != null )
					return Tuple.Create(
						cookie.Value.Any() ? UserManagementStatics.GetUser( int.Parse( cookie.Value ), false ) : null,
						new SpecifiedValue<User>( user ) );
			}

			return Tuple.Create( user, (SpecifiedValue<User>)null );
		}


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
					                       if( errorMessage.Any() )
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
					                       if( errorMessage.Any() )
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
				validationMethod: ( postBackValue, validator ) => clientTime.Value = postBackValue.Value,
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
					                             TimeSpan.FromMinutes( local.AuthenticationTimeoutMinutes.Value )
					                             : user.Role.RequiresEnhancedSecurity
						                             ? TimeSpan.FromMinutes( 12 )
						                             : SessionDuration;

				var ticket = new FormsAuthenticationTicket( user.UserId.ToString(), false /*meaningless*/, (int)authenticationDuration.TotalMinutes );
				AppRequestState.AddNonTransactionalModificationMethod( () => setFormsAuthCookie( ticket ) );
			}
			AppRequestState.Instance.SetUser( user );

			if( identityProvider != null )
				AppRequestState.AddNonTransactionalModificationMethod( () => SetUserLastIdentityProvider( identityProvider ) );
			else
				AppRequestState.AddNonTransactionalModificationMethod( () => CookieStatics.ClearCookie( identityProviderCookieName ) );
		}

		private static void setFormsAuthCookie( FormsAuthenticationTicket ticket ) {
			setCookie( userCookieName, FormsAuthentication.Encrypt( ticket ) );
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
			var cookie = CookieStatics.GetCookie( userCookieName );
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

			AppRequestState.AddNonTransactionalModificationMethod( () => CookieStatics.ClearCookie( identityProviderCookieName ) );
		}

		private static void clearFormsAuthCookie() {
			CookieStatics.ClearCookie( userCookieName );
		}


		// User’s last identity provider

		internal static IdentityProvider GetUserLastIdentityProvider() {
			var cookie = CookieStatics.GetCookie( identityProviderCookieName );

			// Ignore the cookie if the existence of a user has changed since that could mean the user timed out.
			return cookie != null && cookie.Value[ 0 ] == ( AppTools.User != null ? '+' : '-' )
				       ? UserManagementStatics.IdentityProviders.SingleOrDefault(
					       identityProvider => string.Equals(
						       identityProvider is LocalIdentityProvider ? "Local" :
						       identityProvider is SamlIdentityProvider saml ? saml.EntityId : throw new ApplicationException( "identity provider" ),
						       cookie.Value.Substring( 1 ),
						       StringComparison.Ordinal ) )
				       : null;
		}

		internal static void SetUserLastIdentityProvider( IdentityProvider identityProvider ) {
			setCookie(
				identityProviderCookieName,
				( AppTools.User != null ? "+" : "-" ) + ( identityProvider is LocalIdentityProvider ? "Local" :
				                                          identityProvider is SamlIdentityProvider saml ? saml.EntityId :
				                                          throw new ApplicationException( "identity provider" ) ) );
		}


		// Client-side functionality verification

		internal static bool TestCookieMissing() => CookieStatics.GetCookie( testCookieName ) == null;

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