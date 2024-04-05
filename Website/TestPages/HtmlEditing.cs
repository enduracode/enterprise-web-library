// EwlPage

namespace EnterpriseWebLibrary.Website.TestPages {
	partial class HtmlEditing {
		protected override PageContent getContent() =>
			FormState.ExecuteWithDataModificationsAndDefaultAction(
				PostBack.CreateFull().ToCollection(),
				() => new UiPageContent( contentFootActions: new ButtonSetup( "Post Back" ).ToCollection() ).Add(
					new WysiwygHtmlEditor( "", true, ( _, _ ) => {} ).ToFormItem( label: Enumerable.Empty<PhrasingComponent>().Materialize() )
						.ToComponentCollection() ) );
	}
}

namespace EnterpriseWebLibrary.Website.TestPages {
	partial class HtmlEditing {
		protected override UrlHandler getUrlParent() => new LegacyUrlFolderSetup();
	}
}