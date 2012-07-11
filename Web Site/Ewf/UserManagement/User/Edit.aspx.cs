using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayElements.Page;
using RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement;
using RedStapler.StandardLibrary.Validation;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.UserManagement.UserNs {
	public partial class Edit: EwfPage, DataModifierWithRightButton {
		partial class Info {
			protected override void init( DBConnection cn ) {}
			public override string PageName { get { return ""; } }
		}

		protected override void LoadData( DBConnection cn ) {
			userFieldTable.LoadData( cn, es.info.UserId );
		}

		string DataModifierWithRightButton.RightButtonText { get { return "OK"; } }

		void DataModifierWithRightButton.ValidateFormValues( Validator validator ) {
			userFieldTable.ValidateFormValues( validator );
		}

		string DataModifierWithRightButton.ModifyData( DBConnection cn ) {
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
				( UserManagementStatics.SystemProvider as ExternalAuthUserManagementProvider ).InsertOrUpdateUser( cn,
				                                                                                                   es.info.UserId,
				                                                                                                   userFieldTable.Email,
				                                                                                                   userFieldTable.RoleId,
				                                                                                                   es.info.User != null
				                                                                                                   	? es.info.User.LastRequestDateTime
				                                                                                                   	: null );
			}
			userFieldTable.SendEmailIfNecessary();
			return UserManager.Users.GetInfo().GetUrl();
		}
	}
}