using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public class TailUpdateRegion {
		internal readonly IEnumerable<UpdateRegionSet> Sets;
		internal readonly int UpdatingItemCount;

		/// <summary>
		/// Creates a tail update region, which you can use to append items to a list, truncate a list, and/or modify the items at the end of a list.
		/// </summary>
		/// <param name="sets"></param>
		/// <param name="updatingItemCount">Pass zero if you only want to append items.</param>
		public TailUpdateRegion( IEnumerable<UpdateRegionSet> sets, int updatingItemCount ) {
			Sets = sets;
			UpdatingItemCount = updatingItemCount;
		}
	}
}