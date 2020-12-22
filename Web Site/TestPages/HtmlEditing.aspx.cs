using System.Linq;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using Tewl.Tools;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class HtmlEditing: EwfPage {
		protected override PageContent getContent() =>
			FormState.ExecuteWithDataModificationsAndDefaultAction(
				PostBack.CreateFull().ToCollection(),
				() => new UiPageContent( contentFootActions: new ButtonSetup( "Post Back" ).ToCollection() ).Add(
					new HtmlBlockEditor( null, id => {}, out var mod ).ToFormItem( label: Enumerable.Empty<PhrasingComponent>().Materialize() )
						.ToComponentCollection() ) );
	}
}