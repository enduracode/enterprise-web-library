using EnterpriseWebLibrary.EnterpriseWebFramework;

// EwlResource
// Parameter: string text

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class GetImage {
		protected override EwfSafeRequestHandler getOrHead() => new EwfSafeResponseWriter( NetTools.CreateImageFromText( Text, null ) );
	}
}

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class GetImage {
		protected override UrlHandler getUrlParent() => new LegacyUrlFolderSetup();
	}
}