using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.AlternativePageModes;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.Admin {
	public partial class SystemUsers: EwfPage {
		public partial class Info {
			protected override void init( DBConnection cn ) {}

			protected override AlternativePageMode createAlternativeMode() {
				return UserManagementStatics.UserManagementEnabled ? null : new DisabledPageMode( "User management is not enabled in this system." );
			}
		}

		protected override void LoadData( DBConnection cn ) {
			var table = new DynamicTable( new EwfTableColumn( new EwfTableCell( "Email" ), Unit.Percentage( 50 ) ),
			                              new EwfTableColumn( new EwfTableCell( "Role" ), Unit.Percentage( 50 ) ) );
			table.AddActionLink( new ActionButtonSetup( "Create User", new EwfLink( new EditUser.Info( es.info, null ) ) ) );
			foreach( var user in UserManagementStatics.GetUsers( cn ) )
				table.AddTextRow( new RowSetup { ClickScript = ClickScript.CreateRedirectScript( new EditUser.Info( es.info, user.UserId ) ) }, user.Email, user.Role.Name );
			ph.AddControlsReturnThis( table );
		}
	}
}