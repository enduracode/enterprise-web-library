using @@BaseNamespace.Library;

// EwlPage

namespace @@BaseNamespace.Website;

partial class Home {
	protected override IEnumerable<UrlPattern> getChildUrlPatterns() => RequestDispatchingStatics.GetFrameworkUrlPatterns( WebApplicationNames.Website );

	protected override PageContent getContent() => new UiPageContent().Add( new Paragraph( "Welcome to the Enterprise Web Library!".ToComponents() ) );
}