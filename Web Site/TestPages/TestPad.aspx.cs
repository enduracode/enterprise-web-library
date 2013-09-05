using System.Web.UI;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	public partial class TestPad: EwfPage {
		protected override void loadData() {
			ph.AddControlsReturnThis( EwfLink.CreateForNavigationInPopUpWindow( HtmlEditing.GetInfo(), new TextActionControlStyle( "Popup" ),
			new RedStapler.StandardLibrary.JavaScriptWriting.PopUpWindowSettings( "Popup", 800, 600, true, true, true, true ) ) );
			var updatePanel = new UpdatePanel { UpdateMode = UpdatePanelUpdateMode.Always };

			var author = new EwfTextBox( "" ) { AutoPostBack = true };
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
				//author.Value.Length.Times( () => outputArea.AddControlsReturnThis( "Fred".GetLiteralControl() ) );
			};
		}
	}
}
