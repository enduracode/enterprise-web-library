﻿#nullable disable
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
	public Tuple<string, SecondaryPostBackOperation> DmIdAndSecondaryOp { get; set; }
	public Dictionary<string, IEnumerable<string>> InLineModificationErrorsByDisplay { get; }
	public IReadOnlyCollection<TrustedHtmlString> GeneralModificationErrors { get; set; }

	// set before navigation and used to detect possible developer mistakes
	public string StaticRegionContents { get; private set; }
	public IReadOnlyCollection<( string key, string arg )> UpdateRegionKeysAndArguments { get; private set; }

	public PageRequestState( Instant firstRequestTime, string scrollPositionX, string scrollPositionY ) {
		FirstRequestTime = firstRequestTime;
		ScrollPositionX = scrollPositionX;
		ScrollPositionY = scrollPositionY;
		InLineModificationErrorsByDisplay = new Dictionary<string, IEnumerable<string>>();
		GeneralModificationErrors = ImmutableArray<TrustedHtmlString>.Empty;
	}

	public bool ModificationErrorsExist => InLineModificationErrorsByDisplay.Any() || GeneralModificationErrors.Any();

	public bool ModificationErrorsOccurred =>
		ModificationErrorsExist && ( DmIdAndSecondaryOp == null ||
		                             !new[] { SecondaryPostBackOperation.Validate, SecondaryPostBackOperation.ValidateChangesOnly }.Contains(
			                             DmIdAndSecondaryOp.Item2 ) );

	public void SetStaticAndUpdateRegionState( string staticRegionContents, IReadOnlyCollection<( string, string )> updateRegionKeysAndArguments ) {
		StaticRegionContents = staticRegionContents;
		UpdateRegionKeysAndArguments = updateRegionKeysAndArguments;
	}
}