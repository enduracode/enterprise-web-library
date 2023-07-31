// EwlResource
// Parameter: string suffix

namespace EnterpriseWebLibrary.EnterpriseWebFramework.WellKnownUrlHandling;

partial class WellKnownResource {
	private static Func<UrlHandler> frameworkUrlParentGetter = null!;
	private static Func<IEnumerable<WellKnownUrl>> wellKnownUrlGetter = null!;

	internal static void Init( Func<UrlHandler> frameworkUrlParentGetter, Func<IEnumerable<WellKnownUrl>> wellKnownUrlGetter ) {
		WellKnownResource.frameworkUrlParentGetter = frameworkUrlParentGetter;
		WellKnownResource.wellKnownUrlGetter = wellKnownUrlGetter;
	}

	private WellKnownUrl? url;

	protected override void init() {
		if( Suffix.Length > 0 )
			url = wellKnownUrlGetter().Single( i => string.Equals( i.SuffixSegment, Suffix, StringComparison.Ordinal ) );
	}

	protected internal override bool IsIntermediateInstallationPublicResource => true;

	protected override UrlHandler getUrlParent() => Suffix.Length > 0 ? new WellKnownResource( "" ) : frameworkUrlParentGetter();

	protected override IEnumerable<UrlPattern> getChildUrlPatterns() =>
		Suffix.Length > 0
			? Enumerable.Empty<UrlPattern>()
			: new UrlPattern(
				encoder => encoder is UrlEncoder local && local.GetSuffix().Length > 0 ? EncodingUrlSegment.Create( local.GetSuffix() ) : null,
				url => {
					var wellKnownUrl = wellKnownUrlGetter().SingleOrDefault( i => string.Equals( i.SuffixSegment, url.Segment, StringComparison.OrdinalIgnoreCase ) );
					return wellKnownUrl is not null ? new UrlDecoder( suffix: wellKnownUrl.SuffixSegment ) : null;
				} ).ToCollection();

	protected override EwfSafeRequestHandler? getOrHead() => Suffix.Length > 0 ? url!.GetOrHeadHandlerGetter() : base.getOrHead();
}