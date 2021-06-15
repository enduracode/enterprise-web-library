using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Newtonsoft.Json.Linq;
using NodaTime;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal class EwfPageRequestState {
		internal Instant FirstRequestTime { get; }
		internal ImmutableDictionary<string, JToken> ComponentStateValuesById { get; set; }
		internal string ScrollPositionX { get; }
		internal string ScrollPositionY { get; }

		// form data
		internal PostBackValueDictionary PostBackValues { get; set; }

		// set during modifications
		internal string FocusKey { get; set; }
		internal Tuple<string, SecondaryPostBackOperation> DmIdAndSecondaryOp { get; set; }
		internal Dictionary<string, IEnumerable<string>> InLineModificationErrorsByDisplay { get; }
		internal IReadOnlyCollection<TrustedHtmlString> GeneralModificationErrors { get; set; }

		// set before navigation and used to detect possible developer mistakes
		internal string StaticRegionContents { get; private set; }
		internal IEnumerable<Tuple<string, string>> UpdateRegionKeysAndArguments { get; private set; }

		internal EwfPageRequestState( Instant firstRequestTime, string scrollPositionX, string scrollPositionY ) {
			FirstRequestTime = firstRequestTime;
			ScrollPositionX = scrollPositionX;
			ScrollPositionY = scrollPositionY;
			InLineModificationErrorsByDisplay = new Dictionary<string, IEnumerable<string>>();
			GeneralModificationErrors = ImmutableArray<TrustedHtmlString>.Empty;
		}

		internal bool ModificationErrorsExist => InLineModificationErrorsByDisplay.Any() || GeneralModificationErrors.Any();

		internal void SetStaticAndUpdateRegionState( string staticRegionContents, IEnumerable<Tuple<string, string>> updateRegionKeysAndArguments ) {
			StaticRegionContents = staticRegionContents;
			UpdateRegionKeysAndArguments = updateRegionKeysAndArguments;
		}
	}
}