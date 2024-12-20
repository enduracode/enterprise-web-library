// EwlPage

namespace EnterpriseWebLibrary.Website.WebFrameworkDemo {
	partial class HtmlEditing {
		protected override PageContent getContent() =>
			FormState.ExecuteWithActions(
				PostBack.CreateFull(),
				() => new UiPageContent( contentFootActions: new ButtonSetup( "Post Back" ) ).Add(
					new WysiwygHtmlEditor( "", true, ( _, _ ) => {} ).ToFormItem( label: Enumerable.Empty<PhrasingComponent>().Materialize() )
						.ToComponentCollection() ) );
	}
}

namespace EnterpriseWebLibrary.Website.WebFrameworkDemo {
	partial class HtmlEditing {
		protected override UrlHandler getUrlParent() => new LegacyUrlFolderSetup();
	}
}