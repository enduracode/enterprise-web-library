using System.Linq;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.EnterpriseWebFramework.Ui;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class HtmlEditing: EwfPage {
		protected override void loadData() {
			HtmlBlockEditorModification mod;
			FormState.ExecuteWithDataModificationsAndDefaultAction(
				PostBack.CreateFull().ToCollection(),
				() => {
					ph.AddControlsReturnThis(
						new HtmlBlockEditor( null, id => {}, out mod ).ToFormItem( label: Enumerable.Empty<PhrasingComponent>().Materialize() ).ToControl() );
					EwfUiStatics.SetContentFootActions( new ButtonSetup( "Post Back" ).ToCollection() );
				} );
		}
	}
}