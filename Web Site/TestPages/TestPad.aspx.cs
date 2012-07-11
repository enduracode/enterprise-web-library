using System.Web.UI;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

namespace RedStapler.TestWebSite.TestPages {
	public partial class TestPad: EwfPage {
		partial class Info {
			protected override void init( DBConnection cn ) {}
		}

		protected override void LoadData( DBConnection cn ) {
			var updatePanel = new UpdatePanel { UpdateMode = UpdatePanelUpdateMode.Always };

			var author = new EwfTextBox { AutoPostBack = true };
			ScriptManager.GetCurrent( this ).RegisterAsyncPostBackControl( author );

			var outputArea = new Box();

			updatePanel.ContentTemplateContainer.AddControlsReturnThis( outputArea );

			ph.AddControlsReturnThis( updatePanel, author );

			Load += delegate {
				var lifeCycleType = "Not a postback";
				if( ScriptManager.GetCurrent( this ).IsInAsyncPostBack )
					lifeCycleType = "Partial postback";
				else if( IsPostBack )
					lifeCycleType = "Full postback";

				outputArea.AddControlsReturnThis( lifeCycleType.GetLiteralControl() );
				author.Value.Length.Times( () => outputArea.AddControlsReturnThis( "Fred".GetLiteralControl() ) );
			};
		}
	}
}