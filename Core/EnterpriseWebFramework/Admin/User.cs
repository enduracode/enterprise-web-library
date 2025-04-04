using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;
using EnterpriseWebLibrary.UserManagement;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Admin;

// EwlPage
// Parameter: int? userId
partial class User {
	private SystemUser? user;

	protected override void init() {
		if( UserId.HasValue )
			user = UserManagementStatics.GetUser( UserId.Value, true );
	}

	protected override ResourceParent createParent() => new UserManagement( Es );

	protected override string getResourceName() => user == null ? "New User" : user.Email;

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
		UserManagementStatics.SystemProvider.DeleteUser( user!.UserId );
	}
}