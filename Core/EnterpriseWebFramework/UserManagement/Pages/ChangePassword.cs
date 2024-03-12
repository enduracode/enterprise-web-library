using EnterpriseWebLibrary.UserManagement;

// EwlPage
// Parameter: string returnUrl

namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement.Pages;

partial class ChangePassword {
	protected override bool userCanAccess => SystemUser.Current is not null;
	protected override UrlHandler getUrlParent() => new Admin.EntitySetup();

	protected override PageContent getContent() {
		var customContent = AuthenticationStatics.AppProvider.GetChangePasswordPageContent( ReturnUrl );
		if( customContent != null )
			return customContent;

		Action<int>? passwordUpdater = null;
		return FormState.ExecuteWithDataModificationsAndDefaultAction(
			PostBack.CreateFull(
					modificationMethod: () => {
						passwordUpdater!( SystemUser.Current!.UserId );
						AddStatusMessage( StatusMessageType.Info, "Your password has been successfully changed. Use it the next time you log in." );
					},
					actionGetter: () => new PostBackAction( new ExternalResource( ReturnUrl ) ) )
				.ToCollection(),
			() => new UiPageContent(
				pageActions: new HyperlinkSetup( new ExternalResource( ReturnUrl ), "Back" ).ToCollection(),
				contentFootActions: new ButtonSetup( "Change Password" ).ToCollection() ).Add(
				FormItemList.CreateStack()
					.AddItems(
						AuthenticationStatics.GetPasswordModificationFormItems(
							out passwordUpdater,
							firstLabel: "New password".ToComponents(),
							secondLabel: "Re-type new password".ToComponents() ) ) ) );
	}
}