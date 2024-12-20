using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;
using EnterpriseWebLibrary.UserManagement;

// EwlPage
// Parameter: int? userId

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Admin;

partial class SystemUser {
	internal EnterpriseWebLibrary.UserManagement.SystemUser? User { get; private set; }

	protected override void init() {
		if( UserId.HasValue )
			User = UserManagementStatics.GetUser( UserId.Value, true );
	}

	protected override ResourceParent createParent() => new UserManagement( Es );

	protected override string getResourceName() => User == null ? "New User" : User.Email;

	protected override PageContent getContent() {
		Action? userModMethod = null;
		return FormState.ExecuteWithActions(
			PostBack.CreateFull( modificationMethod: () => userModMethod!(), actionGetter: () => new PostBackAction( ParentResource ) ),
			() => new UiPageContent(
				pageActions: UserId.HasValue
					             ? new ButtonSetup(
						             "Delete User",
						             behavior: new PostBackBehavior(
							             postBack: PostBack.CreateFull(
								             id: "delete",
								             modificationMethod: deleteUser,
								             actionGetter: () => new PostBackAction( ParentResource ) ) ) )
					             : null,
				contentFootActions: new ButtonSetup( "OK" ) ).Add( new UserEditor( UserId, out userModMethod ) ) );
	}

	private void deleteUser() {
		UserManagementStatics.SystemProvider.DeleteUser( User!.UserId );
	}
}