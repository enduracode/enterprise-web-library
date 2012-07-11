using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Ui;

namespace RedStapler.TestWebSite.TestPages {
	public partial class HtmlEditing: EwfPage {
		partial class Info {
			protected override void init( DBConnection cn ) {}
		}

		protected override void LoadData( DBConnection cn ) {
			addHtmlEditor( cn );
			EwfUiStatics.SetContentFootActions( new ActionButtonSetup( "Post back", new PostBackButton( new DataModification(), delegate { } ) ) );
		}

		private void addHtmlEditor( DBConnection cn ) {
			var editor = new HtmlBlockEditor();
			editor.LoadData( cn, null );
			ph.AddControlsReturnThis( editor );
			PostBackDataModification.AddValidationMethod( v => editor.ValidateFormValues( v ) );
		}
	}
}