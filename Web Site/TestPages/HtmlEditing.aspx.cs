using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;
using EnterpriseWebLibrary.EnterpriseWebFramework.Ui;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class HtmlEditing: EwfPage {
		protected override void loadData() {
			HtmlBlockEditorModification mod;
			ValidationSetupState.ExecuteWithDataModifications(
				PostBack.CreateFull().ToSingleElementArray(),
				() => {
					ph.AddControlsReturnThis( new HtmlBlockEditor( null, id => { }, out mod ).ToFormItem( "" ).ToControl() );
					EwfUiStatics.SetContentFootActions( new ActionButtonSetup( "Post Back", new PostBackButton() ) );
				} );
		}
	}
}