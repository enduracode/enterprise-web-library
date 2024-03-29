﻿using EnterpriseWebLibrary.EnterpriseWebFramework;

// EwlPage

namespace @@BaseNamespace.Website;

partial class Home {
	protected override IEnumerable<UrlPattern> getChildUrlPatterns() => RequestDispatchingStatics.GetFrameworkUrlPatterns();

	protected override PageContent getContent() => new UiPageContent().Add( new Paragraph( "Welcome to the Enterprise Web Library!".ToComponents() ) );
}