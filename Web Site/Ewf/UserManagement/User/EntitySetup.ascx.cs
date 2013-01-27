using System.Collections.Generic;
using System.Web.UI;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayElements.Entity;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Ui;
using RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement;

// Parameter: int? userId

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.UserManagement.UserNs {
	public partial class EntitySetup: UserControl, EntityDisplaySetup {
		partial class Info {
			public User User { get; private set; }

			protected override void init( DBConnection cn ) {
				if( UserId.HasValue )
					User = UserManagementStatics.GetUser( cn, UserId.Value );
			}

			protected override PageInfo createParentPageInfo() {
				return new UserManager.Users.Info( new UserManager.EntitySetup.Info() );
			}

			protected override List<PageGroup> createPageInfos() {
				return new List<PageGroup>();
			}

			public override string EntitySetupName { get { return User == null ? "New user" : ( "User: " + User.Email ); } }

			protected override bool UserCanAccessEntitySetup { get { return AppTools.User != null && AppTools.User.Role.CanManageUsers; } }
		}

		public void LoadData( DBConnection cn ) {}

		public List<ActionButtonSetup> CreateNavButtonSetups() {
			return new List<ActionButtonSetup>();
		}

		public List<LookupBoxSetup> CreateLookupBoxSetups() {
			return new List<LookupBoxSetup>();
		}

		public List<ActionButtonSetup> CreateActionButtonSetups() {
			var actionButtonSetups = new List<ActionButtonSetup>();
			if( info.User != null ) {
				actionButtonSetups.Add( new ActionButtonSetup( "Delete user",
				                                               new PostBackButton( new DataModification(),
				                                                                   () => EwfPage.Instance.EhModifyDataAndRedirect( cn => {
					                                                                   UserManagementStatics.SystemProvider.DeleteUser( cn, info.User.UserId );
					                                                                   return UserManager.Users.GetInfo().GetUrl();
				                                                                   } ) ) ) );
			}
			return actionButtonSetups;
		}
	}
}