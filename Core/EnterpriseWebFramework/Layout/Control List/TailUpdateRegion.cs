namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public class TailUpdateRegion {
		internal readonly UpdateRegionSet Set;
		internal readonly int UpdatingItemCount;

		/// <summary>
		/// Creates a tail update region, which you can use to append items to a list, truncate a list, and/or modify the items at the end of a list.
		/// </summary>
		/// <param name="set"></param>
		/// <param name="updatingItemCount">Pass zero if you only want to append items.</param>
		public TailUpdateRegion( UpdateRegionSet set, int updatingItemCount ) {
			Set = set;
			UpdatingItemCount = updatingItemCount;
		}
	}
}