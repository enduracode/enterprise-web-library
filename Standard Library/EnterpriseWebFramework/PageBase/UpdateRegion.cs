using System;
using System.Collections.Generic;
using System.Web.UI;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	internal class UpdateRegion {
		private readonly UpdateRegionSet set;
		private readonly Func<IEnumerable<Control>> preModificationControlGetter;
		private readonly Control regionControl;
		private readonly string keySuffix;
		private readonly Func<string> argumentGetter;
		private readonly Func<string, IEnumerable<Control>> postModificationControlGetter;

		internal UpdateRegion( UpdateRegionSet set, Func<IEnumerable<Control>> preModificationControlGetter, Control regionControl, string keySuffix,
		                       Func<string> argumentGetter, Func<string, IEnumerable<Control>> postModificationControlGetter ) {
			this.set = set;
			this.preModificationControlGetter = preModificationControlGetter;
			this.regionControl = regionControl;
			this.keySuffix = keySuffix;
			this.argumentGetter = argumentGetter;
			this.postModificationControlGetter = postModificationControlGetter;
		}

		internal UpdateRegionSet Set { get { return set; } }
		internal Func<IEnumerable<Control>> PreModificationControlGetter { get { return preModificationControlGetter; } }
		internal string Key { get { return regionControl.UniqueID + keySuffix; } }
		internal Func<string> ArgumentGetter { get { return argumentGetter; } }
		internal Func<string, IEnumerable<Control>> PostModificationControlGetter { get { return postModificationControlGetter; } }
	}
}