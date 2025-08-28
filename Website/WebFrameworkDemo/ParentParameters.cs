namespace EnterpriseWebLibrary.Website.WebFrameworkDemo;

// EwlPage
// Parameter: parentUrl
partial class ParentParameters {
	private ResourceParent parent = null!;

	protected override void init() {
		parent = ParentUrl.GetParentOrThrow();
	}

	protected override ResourceParent? createParent() => parent;

	protected override UrlHandler? getUrlParent() => Es;

	protected override PageContent getContent() => new UiPageContent().Add( new Paragraph( "This page has a dynamic parent.".ToComponents() ) );
}