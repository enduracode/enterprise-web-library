using System;
using EnterpriseWebLibrary.WebSessionState;
using Tewl.Tools;

// EwlPage
// Parameter: string returnAndDestinationUrl

namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement.Pages {
	partial class ChangePassword {
		protected override bool userCanAccessResource => AppTools.User != null;
		protected override UrlHandler getUrlParent() => new Admin.EntitySetup();

		protected override PageContent getContent() {
			Action<int> passwordUpdater = null;
			return FormState.ExecuteWithDataModificationsAndDefaultAction(
				PostBack.CreateFull(
						modificationMethod: () => {
							passwordUpdater( AppTools.User.UserId );
							AddStatusMessage( StatusMessageType.Info, "Your password has been successfully changed. Use it the next time you log in." );
						},
						actionGetter: () => new PostBackAction( new ExternalResource( ReturnAndDestinationUrl ) ) )
					.ToCollection(),
				() => new UiPageContent(
					pageActions: new HyperlinkSetup( new ExternalResource( ReturnAndDestinationUrl ), "Back" ).ToCollection(),
					contentFootActions: new ButtonSetup( "Change Password" ).ToCollection() ).Add(
					FormItemList.CreateStack(
						items: AuthenticationStatics.GetPasswordModificationFormItems(
							out passwordUpdater,
							firstLabel: "New password".ToComponents(),
							secondLabel: "Re-type new password".ToComponents() ) ) ) );
		}
	}
}