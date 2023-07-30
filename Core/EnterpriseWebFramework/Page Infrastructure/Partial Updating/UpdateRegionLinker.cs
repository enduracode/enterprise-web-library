#nullable disable
using System;
using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal class UpdateRegionLinker {
		internal readonly string KeySuffix;
		internal readonly IEnumerable<PreModificationUpdateRegion> PreModificationRegions;
		internal readonly Func<string, IEnumerable<PageComponent>> PostModificationRegionGetter;

		internal UpdateRegionLinker(
			string keySuffix, IEnumerable<PreModificationUpdateRegion> preModificationRegions,
			Func<string, IEnumerable<PageComponent>> postModificationRegionGetter ) {
			KeySuffix = keySuffix;
			PreModificationRegions = preModificationRegions;
			PostModificationRegionGetter = postModificationRegionGetter;
		}
	}
}