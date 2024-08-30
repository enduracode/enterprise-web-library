// EwlResource

namespace EnterpriseWebLibrary.Website.WebFrameworkDemo.Basic;

partial class LegacyUrlFolderSetup {
	protected override UrlHandler getUrlParent() => new WebFrameworkDemo.LegacyUrlFolderSetup();
	public override ConnectionSecurity ConnectionSecurity => ConnectionSecurity.MatchingCurrentRequest;

	protected override IEnumerable<UrlPattern> getChildUrlPatterns() {
		var patterns = new List<UrlPattern>();
		patterns.Add(
			new UrlPattern(
				encoder => encoder is ModalContent.UrlEncoder ? EncodingUrlSegment.Create( "ModalContent.aspx" ) : null,
				url => string.Equals( url.Segment, "ModalContent.aspx", StringComparison.OrdinalIgnoreCase ) ? new ModalContent.UrlDecoder() : null ) );
		return patterns;
	}
}