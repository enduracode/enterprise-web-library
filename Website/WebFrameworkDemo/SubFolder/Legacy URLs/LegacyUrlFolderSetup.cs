// EwlResource

namespace EnterpriseWebLibrary.Website.TestPages.SubFolder;

partial class LegacyUrlFolderSetup {
	protected override UrlHandler getUrlParent() => new TestPages.LegacyUrlFolderSetup();
	public override ConnectionSecurity ConnectionSecurity => ConnectionSecurity.MatchingCurrentRequest;

	protected override IEnumerable<UrlPattern> getChildUrlPatterns() {
		var patterns = new List<UrlPattern>();
		patterns.Add(
			new UrlPattern(
				encoder => encoder is Details.UrlEncoder ? EncodingUrlSegment.Create( "Details.aspx" ) : null,
				url => string.Equals( url.Segment, "Details.aspx", StringComparison.OrdinalIgnoreCase ) ? new Details.UrlDecoder() : null ) );
		patterns.Add(
			new UrlPattern(
				encoder => encoder is Disabled.UrlEncoder ? EncodingUrlSegment.Create( "Disabled.aspx" ) : null,
				url => string.Equals( url.Segment, "Disabled.aspx", StringComparison.OrdinalIgnoreCase ) ? new Disabled.UrlDecoder() : null ) );
		patterns.Add(
			new UrlPattern(
				encoder => encoder is General.UrlEncoder ? EncodingUrlSegment.Create( "General.aspx" ) : null,
				url => string.Equals( url.Segment, "General.aspx", StringComparison.OrdinalIgnoreCase ) ? new General.UrlDecoder() : null ) );
		patterns.Add(
			new UrlPattern(
				encoder => encoder is New.UrlEncoder ? EncodingUrlSegment.Create( "New.aspx" ) : null,
				url => string.Equals( url.Segment, "New.aspx", StringComparison.OrdinalIgnoreCase ) ? new New.UrlDecoder() : null ) );
		return patterns;
	}
}