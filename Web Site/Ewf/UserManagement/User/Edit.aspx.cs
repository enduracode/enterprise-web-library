using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Ui;
using RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.UserManagement.UserNs {
	public partial class Edit: EwfPage {
		partial class Info {
			protected override void init( DBConnection cn ) {}
			public override string PageName { get { return ""; } }
		}

		private UserFieldTable userFieldTable;

		protected override void LoadData( DBConnection cn ) {
			var dm = new DataModification();

			userFieldTable = new UserFieldTable();
			userFieldTable.LoadData( cn, es.info.UserId, dm );
			ph.AddControlsReturnThis( userFieldTable );
			EwfUiStatics.SetContentFootActions( new ActionButtonSetup( "OK", new PostBackButton( dm, () => EhRedirect( es.info.ParentPage ) ) ) );

			dm.AddModificationMethod( modifyData );
		}

		private void modifyData( DBConnection cn ) {
			if( UserManagementStatics.SystemProvider is FormsAuthCapableUserManagementProvider ) {
				var provider = UserManagementStatics.SystemProvider as FormsAuthCapableUserManagementProvider;
				if( es.info.UserId.HasValue ) {
					provider.InsertOrUpdateUser( cn,
					                             es.info.User.UserId,
					                             userFieldTable.Email,
					                             userFieldTable.Salt,
					                             userFieldTable.SaltedPassword,
					                             userFieldTable.RoleId,
					                             es.info.User.LastRequestDateTime,
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
				provider.InsertOrUpdateUser( cn, es.info.UserId, userFieldTable.Email, userFieldTable.RoleId, es.info.User != null ? es.info.User.LastRequestDateTime : null );
			}
			userFieldTable.SendEmailIfNecessary();
		}
	}
}