#nullable disable
using System.Collections.Immutable;
using Newtonsoft.Json.Linq;
using NodaTime;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

internal class PageRequestState {
	public Instant FirstRequestTime { get; }
	public ImmutableDictionary<string, JToken> ComponentStateValuesById { get; set; }
	public string ScrollPositionX { get; }
	public string ScrollPositionY { get; }

	// form data
	public PostBackValueDictionary PostBackValues { get; set; }

	// set during modifications
	public string FocusKey { get; set; }
	public Dictionary<string, IEnumerable<string>> InLineModificationErrorsByDisplay { get; } = new();
	public IReadOnlyCollection<TrustedHtmlString> GeneralModificationErrors { get; set; } = Array.Empty<TrustedHtmlString>();

	// set before navigation and used to detect possible developer mistakes
	public string StaticRegionContents { get; private set; }
	public IReadOnlyCollection<( string key, string arg )> UpdateRegionKeysAndArguments { get; private set; }

	public PageRequestState() {
		FirstRequestTime = EwfRequest.Current!.RequestTime;
	}

	public PageRequestState( Instant firstRequestTime, string scrollPositionX, string scrollPositionY ) {
		FirstRequestTime = firstRequestTime;
		ScrollPositionX = scrollPositionX;
		ScrollPositionY = scrollPositionY;
	}

	// Pass null for updateRegionKeysAndArguments when modification errors exist or during the validation stage of an intermediate post-back.
	public void SetStaticAndUpdateRegionState( string staticRegionContents, IReadOnlyCollection<( string, string )> updateRegionKeysAndArguments ) {
		StaticRegionContents = staticRegionContents;
		UpdateRegionKeysAndArguments = updateRegionKeysAndArguments;
	}
}