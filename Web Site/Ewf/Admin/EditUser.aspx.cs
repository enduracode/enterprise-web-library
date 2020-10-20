using System;
using EnterpriseWebLibrary.EnterpriseWebFramework.Ui;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;
using Tewl.Tools;

// Parameter: int? userId

namespace EnterpriseWebLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.Admin {
	partial class EditUser: EwfPage {
		partial class Info {
			internal User User { get; private set; }

			protected override void init() {
				if( UserId.HasValue )
					User = UserManagementStatics.GetUser( UserId.Value, true );
			}

			protected override ResourceBase createParentResource() => new SystemUsers.Info( Es );

			public override string ResourceName => User == null ? "New User" : User.Email;
		}

		protected override void loadData() {
			if( info.UserId.HasValue )
				EwfUiStatics.SetPageActions(
					new ButtonSetup(
						"Delete User",
						behavior: new PostBackBehavior(
							postBack: PostBack.CreateFull(
								id: "delete",
								firstModificationMethod: deleteUser,
								actionGetter: () => new PostBackAction( new SystemUsers.Info( Es ) ) ) ) ).ToCollection() );

			Action userModMethod = null;
			FormState.ExecuteWithDataModificationsAndDefaultAction(
				PostBack.CreateFull( firstModificationMethod: () => userModMethod(), actionGetter: () => new PostBackAction( info.ParentResource ) ).ToCollection(),
				() => {
					ph.AddControlsReturnThis( new UserEditor( info.UserId, out userModMethod ).ToCollection().GetControls() );
					EwfUiStatics.SetContentFootActions( new ButtonSetup( "OK" ).ToCollection() );
				} );
		}

		private void deleteUser() {
			UserManagementStatics.SystemProvider.DeleteUser( info.User.UserId );
		}
	}
}