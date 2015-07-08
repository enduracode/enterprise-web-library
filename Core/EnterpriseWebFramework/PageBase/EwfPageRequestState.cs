using System;
using System.Collections.Generic;
using System.Linq;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	internal class EwfPageRequestState {
		private readonly PageState pageState;
		private readonly string scrollPositionX;
		private readonly string scrollPositionY;

		// set after onLoadData
		internal PostBackValueDictionary PostBackValues { get; set; }

		// set during modifications
		internal string ControlWithInitialFocusId { get; set; }
		internal Tuple<string, SecondaryPostBackOperation> DmIdAndSecondaryOp { get; set; }
		internal IEnumerable<string> TopModificationErrors { get; set; }
		internal Dictionary<string, IEnumerable<string>> InLineModificationErrorsByDisplay { get; private set; }

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

		internal PageState PageState { get { return pageState; } }

		internal string ScrollPositionX { get { return scrollPositionX; } }
		internal string ScrollPositionY { get { return scrollPositionY; } }

		internal bool ModificationErrorsExist { get { return TopModificationErrors.Any(); } }

		internal void SetStaticAndUpdateRegionState( string staticRegionContents, IEnumerable<Tuple<string, string>> updateRegionKeysAndArguments ) {
			StaticRegionContents = staticRegionContents;
			UpdateRegionKeysAndArguments = updateRegionKeysAndArguments;
		}
	}
}