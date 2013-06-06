using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Ui;
using RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement;

// Parameter: int? userId

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.Admin {
	public partial class EditUser: EwfPage {
		public partial class Info {
			internal User User { get; private set; }

			protected override void init( DBConnection cn ) {
				if( UserId.HasValue )
					User = UserManagementStatics.GetUser( cn, UserId.Value );
			}

			protected override PageInfo createParentPageInfo() {
				return new SystemUsers.Info( esInfo );
			}

			public override string PageName { get { return User == null ? "New User" : User.Email; } }
		}

		private UserFieldTable userFieldTable;

		protected override void LoadData( DBConnection cn ) {
			if( info.UserId.HasValue ) {
				EwfUiStatics.SetPageActions( new ActionButtonSetup( "Delete User",
				                                                    new PostBackButton( new DataModification( firstModificationMethod: deleteUser ),
				                                                                        () => EhRedirect( new SystemUsers.Info( es.info ) ) ) ) );
			}

			var dm = new DataModification();

			userFieldTable = new UserFieldTable();
			userFieldTable.LoadData( cn, info.UserId, dm );
			ph.AddControlsReturnThis( userFieldTable );
			EwfUiStatics.SetContentFootActions( new ActionButtonSetup( "OK", new PostBackButton( dm, () => EhRedirect( es.info.ParentPage ) ) ) );

			dm.AddModificationMethod( modifyData );
		}

		private void deleteUser( DBConnection cn ) {
			UserManagementStatics.SystemProvider.DeleteUser( cn, info.User.UserId );
		}

		private void modifyData( DBConnection cn ) {
			if( UserManagementStatics.SystemProvider is FormsAuthCapableUserManagementProvider ) {
				var provider = UserManagementStatics.SystemProvider as FormsAuthCapableUserManagementProvider;
				if( info.UserId.HasValue ) {
					provider.InsertOrUpdateUser( cn,
					                             info.User.UserId,
					                             userFieldTable.Email,
					                             userFieldTable.Salt,
					                             userFieldTable.SaltedPassword,
					                             userFieldTable.RoleId,
					                             info.User.LastRequestDateTime,
					                             userFieldTable.MustChangePassword );
				}
				else {
					provider.InsertOrUpdateUser( cn,
					                             null,
					                             userFieldTable.Email,
					                             userFieldTable.Salt,
					                             userFieldTable.SaltedPassword,
					                             userFieldTable.RoleId,
					                             null,
					                             userFieldTable.MustChangePassword );
				}
			}
			else if( UserManagementStatics.SystemProvider is ExternalAuthUserManagementProvider ) {
				var provider = UserManagementStatics.SystemProvider as ExternalAuthUserManagementProvider;
				provider.InsertOrUpdateUser( cn, info.UserId, userFieldTable.Email, userFieldTable.RoleId, info.User != null ? info.User.LastRequestDateTime : null );
			}
			userFieldTable.SendEmailIfNecessary();
		}
	}
}