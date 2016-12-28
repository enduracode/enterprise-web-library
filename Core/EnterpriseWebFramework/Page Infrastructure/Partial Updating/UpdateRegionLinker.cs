using System;
using System.Collections.Generic;
using System.Web.UI;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal class UpdateRegionLinker {
		internal readonly string KeySuffix;
		internal readonly IEnumerable<PreModificationUpdateRegion> PreModificationRegions;
		internal readonly Func<string, IEnumerable<PageComponent>> PostModificationRegionGetter;

		internal UpdateRegionLinker(
			string keySuffix, IEnumerable<PreModificationUpdateRegion> preModificationRegions, Func<string, IEnumerable<PageComponent>> postModificationRegionGetter ) {
			KeySuffix = keySuffix;
			PreModificationRegions = preModificationRegions;
			PostModificationRegionGetter = postModificationRegionGetter;
		}
	}

	// Web Forms compatibility. Remove when EnduraCode goal 790 is complete.
	internal class LegacyUpdateRegionLinker {
		private readonly Control control;
		private readonly string keySuffix;
		private readonly IEnumerable<LegacyPreModificationUpdateRegion> preModificationRegions;
		private readonly Func<string, IEnumerable<Control>> postModificationRegionGetter;

		internal LegacyUpdateRegionLinker(
			Control control, string keySuffix, IEnumerable<LegacyPreModificationUpdateRegion> preModificationRegions,
			Func<string, IEnumerable<Control>> postModificationRegionGetter ) {
			this.control = control;
			this.keySuffix = keySuffix;
			this.preModificationRegions = preModificationRegions;
			this.postModificationRegionGetter = postModificationRegionGetter;
		}

		internal string Key { get { return control.UniqueID + keySuffix; } }
		internal IEnumerable<LegacyPreModificationUpdateRegion> PreModificationRegions { get { return preModificationRegions; } }
		internal Func<string, IEnumerable<Control>> PostModificationRegionGetter { get { return postModificationRegionGetter; } }
	}
}