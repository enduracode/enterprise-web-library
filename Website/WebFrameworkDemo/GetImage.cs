using EnterpriseWebLibrary.EnterpriseWebFramework;

// EwlResource
// Parameter: string text

namespace EnterpriseWebLibrary.Website.TestPages {
	partial class GetImage {
		protected override EwfSafeRequestHandler getOrHead() => new EwfSafeResponseWriter( NetTools.CreateImageFromText( Text, null ) );
	}
}

namespace EnterpriseWebLibrary.Website.TestPages {
	partial class GetImage {
		protected override UrlHandler getUrlParent() => new LegacyUrlFolderSetup();
	}
}