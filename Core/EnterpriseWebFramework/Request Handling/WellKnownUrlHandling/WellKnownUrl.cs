namespace EnterpriseWebLibrary.EnterpriseWebFramework.WellKnownUrlHandling;

public class WellKnownUrl {
	internal readonly string SuffixSegment;
	internal readonly Func<EwfSafeRequestHandler> GetOrHeadHandlerGetter;

	public WellKnownUrl( string suffixSegment, Func<EwfSafeRequestHandler> getOrHeadHandlerGetter ) {
		SuffixSegment = suffixSegment;
		GetOrHeadHandlerGetter = getOrHeadHandlerGetter;
	}
}