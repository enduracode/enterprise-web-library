using System;
using System.Collections.Generic;
using System.Web.UI;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	// Web Forms compatibility. Remove when EnduraCode goal 790 is complete.
	internal class PlaceholderControl: Control, ControlTreeDataLoader {
		private readonly Func<IEnumerable<Control>> childGetter;

		internal PlaceholderControl( Func<IEnumerable<Control>> childGetter ) {
			this.childGetter = childGetter;
		}

		void ControlTreeDataLoader.LoadData() {
			this.AddControlsReturnThis( childGetter() );
		}
	}
}