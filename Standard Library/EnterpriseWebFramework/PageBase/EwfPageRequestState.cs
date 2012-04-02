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
		internal string[] TopModificationErrors { get; set; }
		internal Dictionary<string, IEnumerable<string>> InLineModificationErrorsByDisplay { get; private set; }

		// set before transfer
		internal string StaticFormControlHash { get; set; }

		internal EwfPageRequestState( PageState pageState, string scrollPositionX, string scrollPositionY ) {
			this.pageState = pageState;
			this.scrollPositionX = scrollPositionX;
			this.scrollPositionY = scrollPositionY;
			PostBackValues = new PostBackValueDictionary( new Dictionary<string, object>() );
			TopModificationErrors = new string[ 0 ];
			InLineModificationErrorsByDisplay = new Dictionary<string, IEnumerable<string>>();
		}

		internal PageState PageState { get { return pageState; } }

		internal string ScrollPositionX { get { return scrollPositionX; } }
		internal string ScrollPositionY { get { return scrollPositionY; } }

		internal bool ModificationErrorsExist { get { return TopModificationErrors.Any(); } }
	}
}