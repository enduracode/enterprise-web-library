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
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement {
	public static class AuthenticationStatics {
		private const string testCookieName = "TestCookie";

		/// <summary>
		/// The idle time required for a session to be erased.
		/// </summary>
		public static readonly TimeSpan SessionDuration = TimeSpan.FromHours( 32 ); // persist across consecutive days of usage

		private static SystemProviderReference<AppAuthenticationProvider> provider;

		private static IReadOnlyCollection<SamlIdentityProvider> samlIdentityProviders;

		internal static void Init( SystemProviderReference<AppAuthenticationProvider> provider ) {
			AuthenticationStatics.provider = provider;
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
							var cookie = CookieStatics.GetCookie( FormsAuthCookieName );
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
		/// Gets password and "password again" form items. The validation sets this data value to the provided password, and ensures that the two form items contain
		/// identical, valid passwords.
		/// </summary>
		public static IReadOnlyCollection<FormItem> GetPasswordModificationFormItems(
			this DataValue<string> password, IEnumerable<PhrasingComponent> firstLabel = null, IEnumerable<PhrasingComponent> secondLabel = null ) {
			var passwordAgain = new DataValue<string>();
			var passwordAgainFormItem = passwordAgain.ToTextControl( true, setup: TextControlSetup.CreateObscured( autoFillTokens: "new-password" ), value: "" )
				.ToFormItem( label: secondLabel?.Materialize() ?? "Password again".ToComponents() );

			var passwordFormItem = password.ToTextControl(
					true,
					setup: TextControlSetup.CreateObscured( autoFillTokens: "new-password" ),
					value: "",
					additionalValidationMethod: validator => {
						if( password.Value != passwordAgain.Value )
							validator.NoteErrorAndAddMessage( "Passwords do not match." );
						else {
							if( UserManagementStatics.LocalIdentityProvider.PasswordValidationMethod != null )
								UserManagementStatics.LocalIdentityProvider.PasswordValidationMethod( validator, password.Value );
							else if( password.Value.Length < 7 )
								validator.NoteErrorAndAddMessage( "Passwords must be at least 7 characters long." );
						}
					} )
				.ToFormItem( label: firstLabel?.Materialize() ?? "Password".ToComponents() );

			return new[] { passwordFormItem, passwordAgainFormItem };
		}


		// Log-In

		/// <summary>
		/// Gets an email address form item for use on log-in pages.
		/// </summary>
		public static FormItem GetEmailAddressFormItem( this DataValue<string> emailAddress, IReadOnlyCollection<PhrasingComponent> label ) =>
			emailAddress.ToEmailAddressControl( false, setup: EmailAddressControlSetup.Create( autoFillTokens: "email" ), value: "" ).ToFormItem( label: label );

		/// <summary>
		/// Returns log-in hidden fields and a modification method that logs in a user. Also sets up client-side logic for user log-in. Do not call if the local
		/// identity provider is not enabled.
		/// </summary>
		public static Tuple<IReadOnlyCollection<EtherealComponent>, Func<( User, bool )>> GetLogInHiddenFieldsAndMethod(
			DataValue<string> emailAddress, DataValue<string> password, string emailAddressErrorMessage, string passwordErrorMessage ) {
			var clientTime = new DataValue<string>();
			var hiddenFields = getLogInHiddenFieldsAndSetUpClientSideLogic( clientTime );

			return Tuple.Create(
				hiddenFields,
				new Func<( User, bool )>(
					() => {
						var errors = new List<string>();

						var errorMessage = UserManagementStatics.LocalIdentityProvider.LogInUserWithPassword(
							emailAddress.Value,
							password.Value,
							emailAddressErrorMessage,
							passwordErrorMessage,
							out var user,
							out var mustChangePassword );
						if( errorMessage.Any() )
							errors.Add( errorMessage );
						else
							SetFormsAuthCookieAndUser( user, authenticationTimeoutMinutes: UserManagementStatics.LocalIdentityProvider.AuthenticationTimeoutMinutes );

						errors.AddRange( verifyTestCookie() );
						addStatusMessageIfClockNotSynchronized( clientTime );

						if( errors.Any() )
							throw new DataModificationException( errors.ToArray() );
						return ( user, mustChangePassword );
					} ) );
		}

		/// <summary>
		/// Returns log-in hidden fields and a modification method that logs in the specified user. Also sets up client-side logic for user log-in. Do not call if
		/// user management is not enabled.
		/// </summary>
		public static Tuple<IReadOnlyCollection<EtherealComponent>, Action<int>> GetLogInHiddenFieldsAndSpecifiedUserLogInMethod() {
			var clientTime = new DataValue<string>();
			var hiddenFields = getLogInHiddenFieldsAndSetUpClientSideLogic( clientTime );

			return Tuple.Create(
				hiddenFields,
				new Action<int>(
					userId => {
						var user = UserManagementStatics.SystemProvider.GetUser( userId );
						SetFormsAuthCookieAndUser( user );

						var errors = new List<string>();
						errors.AddRange( verifyTestCookie() );
						addStatusMessageIfClockNotSynchronized( clientTime );
						if( errors.Any() )
							throw new DataModificationException( errors.ToArray() );
					} ) );
		}

		private static IReadOnlyCollection<EtherealComponent> getLogInHiddenFieldsAndSetUpClientSideLogic( DataValue<string> clientTime ) {
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
		public static void SetFormsAuthCookieAndUser( User user, int? authenticationTimeoutMinutes = null ) {
			if( AppRequestState.Instance.ImpersonatorExists )
				UserImpersonationStatics.SetCookie( user );
			else {
				// If the user's role requires enhanced security, require re-authentication every 12 minutes. Otherwise, make it the same as a session timeout.
				var authenticationDuration = authenticationTimeoutMinutes.HasValue ? TimeSpan.FromMinutes( authenticationTimeoutMinutes.Value ) :
				                             user.Role.RequiresEnhancedSecurity ? TimeSpan.FromMinutes( 12 ) : SessionDuration;

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

		private static void addStatusMessageIfClockNotSynchronized( DataValue<string> clientTime ) {
			var clientParseResult = InstantPattern.ExtendedIso.Parse( clientTime.Value );
			if( !clientParseResult.Success )
				throw new DataModificationException( "Your browser did not submit the current time." );

			var clockDifference = clientParseResult.GetValueOrThrow() - AppRequestState.RequestTime;
			if( Math.Abs( clockDifference.TotalMinutes ) > 5 ) {
				var timeZone = DateTimeZoneProviders.Tzdb.GetSystemDefault();
				PageBase.AddStatusMessage(
					StatusMessageType.Warning,
					Translation.YourClockIsWrong + " " + AppRequestState.RequestTime.InZone( timeZone ).ToDateTimeUnspecified().ToHourAndMinuteString() + " " +
					timeZone.GetZoneInterval( AppRequestState.RequestTime ).Name + "." );
			}
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

		internal static string FormsAuthCookieName => "User";
	}
}