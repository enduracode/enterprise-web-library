using EnterpriseWebLibrary.EnterpriseWebFramework;

// EwlPage

namespace EnterpriseWebLibrary.Website.TestPages.SubFolder {
	partial class New {
		protected override AlternativeResourceMode createAlternativeMode() => new NewContentResourceMode();
	}
}

namespace EnterpriseWebLibrary.Website.TestPages.SubFolder {
	partial class New {
		protected override UrlHandler getUrlParent() => new LegacyUrlFolderSetup();
	}
}