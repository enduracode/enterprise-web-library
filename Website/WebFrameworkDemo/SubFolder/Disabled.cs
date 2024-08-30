using EnterpriseWebLibrary.EnterpriseWebFramework;

// EwlPage

namespace EnterpriseWebLibrary.Website.TestPages.SubFolder {
	partial class Disabled {
		protected override AlternativeResourceMode createAlternativeMode() => new DisabledResourceMode( "Disabled!" );
	}
}

namespace EnterpriseWebLibrary.Website.TestPages.SubFolder {
	partial class Disabled {
		protected override UrlHandler getUrlParent() => new LegacyUrlFolderSetup();
	}
}