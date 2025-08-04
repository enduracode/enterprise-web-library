using EnterpriseWebLibrary.EnterpriseWebFramework.Core.ResourceMetaLogic;
using EnterpriseWebLibrary.UserManagement;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement.Pages;

// EwlPage
// Parameter: string returnUrl
partial class ChangePassword {
	protected override bool userCanAccess => SystemUser.Current is not null;
	protected override UrlHandler getUrlParent() => new Admin.EntitySetup();

	protected override PageContent getContent() {
		var customContent = AuthenticationStatics.AppProvider.GetChangePasswordPageContent( ReturnUrl );
		if( customContent != null )
			return customContent;

		Action<int>? passwordUpdater = null;
		return FormState.ExecuteWithActions(
			PostBack.CreateFull(
				modificationMethod: () => {
					passwordUpdater!( SystemUser.Current!.UserId );
					AddStatusMessage( StatusMessageType.Info, "Your password has been successfully changed. Use it the next time you log in." );
				},
				actionGetter: () => new PostBackAction( new ExternalResource( ReturnUrl ) ) ),
			() => new UiPageContent(
				pageActions: new HyperlinkSetup( new ExternalResource( ReturnUrl ), "Back" ),
				contentFootActions: new ButtonSetup( "Change Password" ) ).Add(
				FormItemList.CreateStack()
					.AddItems(
						AuthenticationStatics.GetPasswordModificationFormItems(
							SystemUser.Current!.UserId,
							out passwordUpdater,
							firstLabel: "New password".ToComponents(),
							secondLabel: "Re-type new password".ToComponents() ) ) ) );
	}
}