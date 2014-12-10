using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.Admin {
	partial class SystemUsers: EwfPage {
		partial class Info {
			protected override AlternativeResourceMode createAlternativeMode() {
				return UserManagementStatics.UserManagementEnabled ? null : new DisabledResourceMode( "User management is not enabled in this system." );
			}
		}

		protected override void loadData() {
			var table = new DynamicTable( new EwfTableColumn( "Email", Unit.Percentage( 50 ) ), new EwfTableColumn( "Role", Unit.Percentage( 50 ) ) );
			table.AddActionLink( new ActionButtonSetup( "Create User", new EwfLink( new EditUser.Info( es.info, null ) ) ) );
			foreach( var user in UserManagementStatics.GetUsers() )
				table.AddTextRow( new RowSetup { ClickScript = ClickScript.CreateRedirectScript( new EditUser.Info( es.info, user.UserId ) ) }, user.Email, user.Role.Name );
			ph.AddControlsReturnThis( table );
		}
	}
}