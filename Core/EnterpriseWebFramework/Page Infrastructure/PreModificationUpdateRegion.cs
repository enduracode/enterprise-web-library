using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Web.UI;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal class PreModificationUpdateRegion {
		private readonly IEnumerable<UpdateRegionSet> sets;
		private readonly Func<IEnumerable<Control>> controlGetter;
		private readonly Func<string> argumentGetter;

		internal PreModificationUpdateRegion( IEnumerable<UpdateRegionSet> sets, Func<IEnumerable<Control>> controlGetter, Func<string> argumentGetter ) {
			this.sets = sets ?? ImmutableArray<UpdateRegionSet>.Empty;
			this.controlGetter = controlGetter;
			this.argumentGetter = argumentGetter;
		}

		internal IEnumerable<UpdateRegionSet> Sets { get { return sets; } }
		internal Func<IEnumerable<Control>> ControlGetter { get { return controlGetter; } }
		internal Func<string> ArgumentGetter { get { return argumentGetter; } }
	}
}