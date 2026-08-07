using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure.ElementBase.Classification;
using EnterpriseWebLibrary.EnterpriseWebFramework.Core.ResourceMetaLogic;
using EnterpriseWebLibrary.UserManagement;
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement.Pages;

// EwlPage
// Parameter: string returnUrl // Not a TrustedUrl because that would cause intermediate-installation links to expire, making automated testing more difficult.
// OptionalParameter: string user
partial class Impersonate {
	internal const string AnonymousUser = "anonymous";
	private static readonly ElementClass elementClass = new( "ewfSelectUser" );

	[ UsedImplicitly ]
	private class CssElementCreator: ControlCssElementCreator {
		IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() =>
			new CssElement( "SelectUserPageBody", "body.{0}".FormatWith( elementClass.ClassName ) ).ToCollection();
	}

	private TrustedResourceInfo? returnResource;
	private SystemUser? userObject;

	protected override void init() {
		if( !UserManagementStatics.UserManagementEnabled )
			throw new Exception( "User management not enabled" );

		if( ReturnUrl.Length > 0 )
			returnResource = new TrustedExternalResource( new ExternalResource( ReturnUrl ) );

		if( User.Any() ) {
			if( returnResource is null )
				throw new Exception( "no return URL with user" );
			if( User != AnonymousUser && ( userObject = UserManagementStatics.SystemProvider.GetUser( User ) ) == null )
				throw new Exception( "user" );
		}
	}

	protected override string getResourceName() => ConfigurationStatics.IsLiveInstallation ? "Impersonate User" : "Select User";

	protected override bool userCanAccess {
		get {
			var user = RequestState.Instance.ImpersonatorExists ? RequestState.Instance.ImpersonatorUser : SystemUser.Current;
			return AuthenticationStatics.UserCanImpersonate( user );
		}
	}

	protected override UrlHandler getUrlParent() => new Admin.EntitySetup();

	// This page does not use the EWF UI because displaying authenticated user information would be misleading.
	protected override PageContent getContent() {
		if( User.Any() )
			return new BasicPageContent(
				bodyClasses: elementClass,
				pageLoadPostBack: PostBack.CreateFull(
					modificationMethod: () => UserImpersonationStatics.BeginImpersonation( userObject ),
					actionGetter: () => new PostBackAction( returnResource ) ) );

		var content = new BasicPageContent( bodyClasses: elementClass );
		content.Add( new PageName() );

		if( ConfigurationStatics.IsLiveInstallation )
			content.Add(
				new Paragraph(
					new ImportantContent( "Warning:".ToComponents() ).ToCollection()
						.Concat(
							" Do not impersonate a user without permission. Your actions will be attributed to the user you are impersonating, not to you.".ToComponents() )
						.Materialize() ) );

		var user = new DataValue<SystemUser?>( false, null, new SpecifiedValue<SystemUser?>( null ) );
		var pb = PostBack.CreateFull(
			modificationMethod: () => UserImpersonationStatics.BeginImpersonation( user.Value ),
			actionGetter: () => new PostBackAction(
				returnResource ?? EwfConfigurationStatics.GetDefaultBaseResource(),
				authorizationCheckDisabledPredicate: _ => true ) );
		FormState.ExecuteWithActions(
			pb,
			() => {
				var multipleValuesEntered = false;
				content.Add(
						FormItemList.CreateWrapping()
							.AddItem(
								new EmailAddressControl(
									"",
									true,
									setup: EmailAddressControlSetup.Create( validationErrorNotifier: () => user.Value = null ),
									validationMethod: ( value, validator ) => {
										if( !value.Any() )
											return;
										user.Value = UserManagementStatics.SystemProvider.GetUser( value );
										if( user.Value is null )
											validator.NoteErrorAndAddMessage( "The email address you entered does not match a user." );
									} ).ToFormItem( label: "User’s email address".ToComponents() ) )
							.AddItem(
								new NumericTextControl(
									"",
									true,
									setup: NumericTextControlSetup.Create( validationErrorNotifier: notifySubsequentFieldValueEntered ),
									minLength: 1,
									maxLength: 10,
									validationMethod: ( value, validator ) => {
										if( value.Any() )
											notifySubsequentFieldValueEntered();
										else
											return;
										user.Value = UserManagementStatics.SystemProvider.GetUser( int.Parse( value ) );
										if( user.Value is null )
											validator.NoteErrorAndAddMessage( "The ID you entered does not match a user." );
									} ).ToFormItem( label: "Or user’s ID".ToComponents() ) ) )
					.Add( new SideComments( "(leave both fields blank for anonymous)".ToComponents() ) )
					.Add(
						new FlowErrorContainer(
							new ErrorSourceSet(
								validations: new EwfValidation( validator => {
									if( multipleValuesEntered )
										validator.NoteErrorAndAddMessage( "Please enter only one of the above." );
								} ).ToCollection() ),
							new ListErrorDisplayStyle() ) )
					.Add(
						new Paragraph(
							new EwfButton(
									new StandardButtonStyle( RequestState.Instance.ImpersonatorExists ? "Change User" : "Begin Impersonation", buttonSize: ButtonSize.Large ) )
								.ToCollection() ) );
				return;

				void notifySubsequentFieldValueEntered() => multipleValuesEntered = user.HasChanged;
			} );

		return content;
	}
}