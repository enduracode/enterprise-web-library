using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;
using EnterpriseWebLibrary.EnterpriseWebFramework.Ui;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class HtmlEditing: EwfPage {
		protected override void loadData() {
			addHtmlEditor();
			EwfUiStatics.SetContentFootActions( new ActionButtonSetup( "Post back", new PostBackButton( PostBack.CreateFull() ) ) );
		}

		private void addHtmlEditor() {
			HtmlBlockEditorModification mod;
			ph.AddControlsReturnThis( new HtmlBlockEditor( null, id => { }, out mod ).ToFormItem( "" ).ToControl() );
		}
	}
}