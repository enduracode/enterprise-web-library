using EnterpriseWebLibrary.EnterpriseWebFramework;

// EwlPage

namespace EnterpriseWebLibrary.WebSite.TestPages.SubFolder {
	partial class Disabled {
		protected override AlternativeResourceMode createAlternativeMode() => new DisabledResourceMode( "Disabled!" );
	}
}

namespace EnterpriseWebLibrary.WebSite.TestPages.SubFolder {
	partial class Disabled {
		protected override UrlHandler getUrlParent() => new LegacyUrlFolderSetup();
	}
}