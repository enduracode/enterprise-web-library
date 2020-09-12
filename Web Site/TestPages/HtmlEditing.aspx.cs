using System.Linq;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.EnterpriseWebFramework.Ui;
using Tewl.Tools;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class HtmlEditing: EwfPage {
		protected override void loadData() {
			FormState.ExecuteWithDataModificationsAndDefaultAction(
				PostBack.CreateFull().ToCollection(),
				() => {
					ph.AddControlsReturnThis(
						new HtmlBlockEditor( null, id => {}, out var mod ).ToFormItem( label: Enumerable.Empty<PhrasingComponent>().Materialize() )
							.ToComponentCollection()
							.GetControls() );
					EwfUiStatics.SetContentFootActions( new ButtonSetup( "Post Back" ).ToCollection() );
				} );
		}
	}
}