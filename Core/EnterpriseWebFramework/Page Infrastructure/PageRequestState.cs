using System.Collections.Immutable;
using Newtonsoft.Json.Linq;
using NodaTime;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

internal class PageRequestState {
	public Instant FirstRequestTime { get; }
	public ImmutableDictionary<string, JToken>? ComponentStateValuesById { get; set; }
	public string? ScrollPositionX { get; }
	public string? ScrollPositionY { get; }

	// form data
	public PostBackValueDictionary? PostBackValues { get; set; }

	// set during modifications
	public Dictionary<string, IEnumerable<string>> InLineModificationErrorsByDisplay { get; } = new();
	public IReadOnlyCollection<TrustedHtmlString> GeneralModificationErrors { get; set; } = Array.Empty<TrustedHtmlString>();

	public PageRequestState() {
		FirstRequestTime = EwfRequest.Current!.RequestTime;
	}

	public PageRequestState( Instant firstRequestTime, string scrollPositionX, string scrollPositionY ) {
		FirstRequestTime = firstRequestTime;
		ScrollPositionX = scrollPositionX;
		ScrollPositionY = scrollPositionY;
	}
}