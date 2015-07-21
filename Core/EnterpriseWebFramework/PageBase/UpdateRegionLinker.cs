using System;
using System.Collections.Generic;
using System.Web.UI;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal class UpdateRegionLinker {
		private readonly Control control;
		private readonly string keySuffix;
		private readonly IEnumerable<PreModificationUpdateRegion> preModificationRegions;
		private readonly Func<string, IEnumerable<Control>> postModificationRegionGetter;

		internal UpdateRegionLinker( Control control, string keySuffix, IEnumerable<PreModificationUpdateRegion> preModificationRegions,
		                             Func<string, IEnumerable<Control>> postModificationRegionGetter ) {
			this.control = control;
			this.keySuffix = keySuffix;
			this.preModificationRegions = preModificationRegions;
			this.postModificationRegionGetter = postModificationRegionGetter;
		}

		internal string Key { get { return control.UniqueID + keySuffix; } }
		internal IEnumerable<PreModificationUpdateRegion> PreModificationRegions { get { return preModificationRegions; } }
		internal Func<string, IEnumerable<Control>> PostModificationRegionGetter { get { return postModificationRegionGetter; } }
	}
}