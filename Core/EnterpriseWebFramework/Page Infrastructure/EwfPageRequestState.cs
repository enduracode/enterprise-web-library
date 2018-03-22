using System;
using System.Collections.Generic;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal class EwfPageRequestState {
		private readonly PageState pageState;
		private readonly string scrollPositionX;
		private readonly string scrollPositionY;

		// set after onLoadData
		internal PostBackValueDictionary PostBackValues { get; set; }

		// set during modifications
		internal string FocusKey { get; set; }
		internal Tuple<string, SecondaryPostBackOperation> DmIdAndSecondaryOp { get; set; }
		internal IEnumerable<string> TopModificationErrors { get; set; }
		internal Dictionary<string, IEnumerable<string>> InLineModificationErrorsByDisplay { get; }

		// set before navigation and used to detect possible developer mistakes
		internal string StaticRegionContents { get; private set; }
		internal IEnumerable<Tuple<string, string>> UpdateRegionKeysAndArguments { get; private set; }

		internal EwfPageRequestState( PageState pageState, string scrollPositionX, string scrollPositionY ) {
			this.pageState = pageState;
			this.scrollPositionX = scrollPositionX;
			this.scrollPositionY = scrollPositionY;
			TopModificationErrors = new string[ 0 ];
			InLineModificationErrorsByDisplay = new Dictionary<string, IEnumerable<string>>();
		}

		internal PageState PageState => pageState;

		internal string ScrollPositionX => scrollPositionX;
		internal string ScrollPositionY => scrollPositionY;

		internal bool ModificationErrorsExist => TopModificationErrors.Any();

		internal void SetStaticAndUpdateRegionState( string staticRegionContents, IEnumerable<Tuple<string, string>> updateRegionKeysAndArguments ) {
			StaticRegionContents = staticRegionContents;
			UpdateRegionKeysAndArguments = updateRegionKeysAndArguments;
		}
	}
}