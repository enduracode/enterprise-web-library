using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Ui;
using RedStapler.StandardLibrary.Validation;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	public partial class HtmlEditing: EwfPage {
		protected override void loadData() {
			addHtmlEditor();
			EwfUiStatics.SetContentFootActions( new ActionButtonSetup( "Post back", new PostBackButton( PostBack.CreateFull() ) ) );
		}

		private void addHtmlEditor() {
			HtmlBlockEditorModification mod;
			ph.AddControlsReturnThis(
				FormItem.Create( null,
				                 new HtmlBlockEditor( null, id => { }, out mod ),
				                 validationGetter: c => new Validation( ( pbv, v ) => c.Validate( pbv, v, new ValidationErrorHandler( "html" ) ), DataUpdate ) ).ToControl() );
		}
	}
}