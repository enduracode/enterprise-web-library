using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;
using EnterpriseWebLibrary.EnterpriseWebFramework.Ui;
using EnterpriseWebLibrary.InputValidation;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	public partial class HtmlEditing: EwfPage {
		protected override void loadData() {
			addHtmlEditor();
			EwfUiStatics.SetContentFootActions( new ActionButtonSetup( "Post back", new PostBackButton( PostBack.CreateFull() ) ) );
		}

		private void addHtmlEditor() {
			HtmlBlockEditorModification mod;
			ph.AddControlsReturnThis(
				FormItem.Create(
					"",
					new HtmlBlockEditor( null, id => { }, out mod ),
					validationGetter: c => new Validation( ( pbv, v ) => c.Validate( pbv, v, new ValidationErrorHandler( "html" ) ), DataUpdate ) ).ToControl() );
		}
	}
}