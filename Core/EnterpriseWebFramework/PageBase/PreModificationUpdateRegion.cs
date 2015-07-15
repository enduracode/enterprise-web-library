using System;
using System.Collections.Generic;
using System.Web.UI;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal class PreModificationUpdateRegion {
		private readonly UpdateRegionSet set;
		private readonly Func<IEnumerable<Control>> controlGetter;
		private readonly Func<string> argumentGetter;

		internal PreModificationUpdateRegion( UpdateRegionSet set, Func<IEnumerable<Control>> controlGetter, Func<string> argumentGetter ) {
			this.set = set;
			this.controlGetter = controlGetter;
			this.argumentGetter = argumentGetter;
		}

		internal UpdateRegionSet Set { get { return set; } }
		internal Func<IEnumerable<Control>> ControlGetter { get { return controlGetter; } }
		internal Func<string> ArgumentGetter { get { return argumentGetter; } }
	}
}