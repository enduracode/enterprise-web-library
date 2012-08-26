using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Ui;
using RedStapler.StandardLibrary.Validation;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	public partial class HtmlEditing: EwfPage {
		partial class Info {
			protected override void init( DBConnection cn ) {}
		}

		protected override void LoadData( DBConnection cn ) {
			addHtmlEditor();
			EwfUiStatics.SetContentFootActions( new ActionButtonSetup( "Post back", new PostBackButton( new DataModification(), delegate { } ) ) );
		}

		private void addHtmlEditor() {
			HtmlBlockEditorModification mod;
			ph.AddControlsReturnThis(
				FormItem.Create( "",
				                 new HtmlBlockEditor( null, id => { }, out mod ),
				                 validationGetter: c => new Validation( ( pbv, v ) => c.Validate( pbv, v, new ValidationErrorHandler( "html" ) ), PostBackDataModification ) )
					.ToControl() );
		}
	}
}