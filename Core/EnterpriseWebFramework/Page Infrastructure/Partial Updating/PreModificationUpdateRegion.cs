#nullable disable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal class PreModificationUpdateRegion {
		internal readonly IEnumerable<UpdateRegionSet> Sets;
		internal readonly Func<IEnumerable<PageComponent>> ComponentGetter;
		internal readonly Func<string> ArgumentGetter;

		internal PreModificationUpdateRegion( IEnumerable<UpdateRegionSet> sets, Func<IEnumerable<PageComponent>> componentGetter, Func<string> argumentGetter ) {
			Sets = sets ?? ImmutableArray<UpdateRegionSet>.Empty;
			ComponentGetter = componentGetter;
			ArgumentGetter = argumentGetter;
		}
	}
}