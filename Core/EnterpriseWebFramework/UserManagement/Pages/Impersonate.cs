using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.UserManagement;
using JetBrains.Annotations;

// EwlPage
// Parameter: string returnUrl
// OptionalParameter: string user

namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement.Pages {
	// This page does not use the EWF UI because displaying authenticated user information would be misleading.
	partial class Impersonate {
		internal const string AnonymousUser = "anonymous";
		private static readonly ElementClass elementClass = new( "ewfSelectUser" );

		[ UsedImplicitly ]
		private class CssElementCreator: ControlCssElementCreator {
			IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() =>
				new CssElement( "SelectUserPageBody", "body.{0}".FormatWith( elementClass.ClassName ) ).ToCollection();
		}

		internal User UserObject { get; private set; }

		protected override void init() {
			if( !UserManagementStatics.UserManagementEnabled )
				throw new ApplicationException( "User management not enabled" );

			if( User.Any() && User != AnonymousUser && ( UserObject = UserManagementStatics.SystemProvider.GetUser( User ) ) == null )
				throw new ApplicationException( "user" );
		}

		public override string ResourceName => ConfigurationStatics.IsLiveInstallation ? "Impersonate User" : "Select User";

		protected override bool userCanAccessResource {
			get {
				var user = AppRequestState.Instance.ImpersonatorExists ? AppRequestState.Instance.ImpersonatorUser : AppTools.User;
				return AuthenticationStatics.UserCanImpersonate( user );
			}
		}

		protected override UrlHandler getUrlParent() => new Admin.EntitySetup();

		protected override PageContent getContent() {
			if( User.Any() )
				return new BasicPageContent(
					bodyClasses: elementClass,
					pageLoadPostBack: PostBack.CreateFull(
						modificationMethod: () => UserImpersonationStatics.BeginImpersonation( UserObject ),
						actionGetter: () => new PostBackAction( new ExternalResource( ReturnUrl ) ) ) ).Add( new Paragraph( "Please wait.".ToComponents() ) );

			var content = new BasicPageContent( bodyClasses: elementClass );
			content.Add( new PageName() );

			if( ConfigurationStatics.IsLiveInstallation )
				content.Add(
					new Paragraph(
						new ImportantContent( "Warning:".ToComponents() ).ToCollection()
							.Concat(
								" Do not impersonate a user without permission. Your actions will be attributed to the user you are impersonating, not to you.".ToComponents() )
							.Materialize() ) );

			var user = new DataValue<User>();
			var pb = PostBack.CreateFull(
				modificationMethod: () => UserImpersonationStatics.BeginImpersonation( user.Value ),
				actionGetter: () => new PostBackAction(
					new ExternalResource(
						ReturnUrl.Any()
							? ReturnUrl
							: EwfConfigurationStatics.AppConfiguration.DefaultBaseUrl.GetUrlString( EwfConfigurationStatics.AppSupportsSecureConnections ) ) ) );
			FormState.ExecuteWithDataModificationsAndDefaultAction(
				pb.ToCollection(),
				() => {
					content.Add(
						new EmailAddressControl(
								"",
								true,
								validationMethod: ( postBackValue, validator ) => {
									if( !postBackValue.Any() ) {
										user.Value = null;
										return;
									}
									user.Value = UserManagementStatics.SystemProvider.GetUser( postBackValue );
									if( user.Value == null )
										validator.NoteErrorAndAddMessage( "The email address you entered does not match a user." );
								} ).ToFormItem( label: "User's email address (leave blank for anonymous)".ToComponents() )
							.ToComponentCollection()
							.Append(
								new Paragraph(
									new EwfButton(
											new StandardButtonStyle(
												AppRequestState.Instance.ImpersonatorExists ? "Change User" : "Begin Impersonation",
												buttonSize: ButtonSize.Large ) )
										.ToCollection() ) )
							.Materialize() );
				} );

			return content;
		}
	}
}