using EnterpriseWebLibrary.EnterpriseWebFramework;

// EwlPage

namespace EnterpriseWebLibrary.Website.WebFrameworkDemo.SubFolder {
	partial class Disabled {
		protected override AlternativeResourceMode createAlternativeMode() => new DisabledResourceMode( "Disabled!" );
	}
}

namespace EnterpriseWebLibrary.Website.WebFrameworkDemo.SubFolder {
	partial class Disabled {
		protected override UrlHandler getUrlParent() => new LegacyUrlFolderSetup();
	}
}