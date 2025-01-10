using @@BaseNamespace.Library;

namespace @@BaseNamespace.Website;

// EwlPage
partial class Home {
	protected override string getResourceName() => "";

	protected override IEnumerable<UrlPattern> getChildUrlPatterns() => RequestDispatchingStatics.GetFrameworkUrlPatterns( WebApplicationNames.Website );

	protected override PageContent getContent() => new UiPageContent().Add( new Paragraph( "Welcome to the Enterprise Web Library!".ToComponents() ) );
}