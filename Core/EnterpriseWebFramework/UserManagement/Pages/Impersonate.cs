using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure.ElementBase.Classification;
using EnterpriseWebLibrary.EnterpriseWebFramework.Core.ResourceMetaLogic;
using EnterpriseWebLibrary.UserManagement;
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement.Pages;

// EwlPage
// Parameter: ? returnUrl
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

		ReturnUrl.TryGetResource( out returnResource );

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

		var user = new DataValue<SystemUser?>( false );
		var pb = PostBack.CreateFull(
			modificationMethod: () => UserImpersonationStatics.BeginImpersonation( user.Value ),
			actionGetter: () => new PostBackAction( returnResource ?? EwfConfigurationStatics.GetDefaultBaseResource() ) );
		FormState.ExecuteWithActions(
			pb,
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
										new StandardButtonStyle( RequestState.Instance.ImpersonatorExists ? "Change User" : "Begin Impersonation", buttonSize: ButtonSize.Large ) )
									.ToCollection() ) )
						.Materialize() );
			} );

		return content;
	}
}