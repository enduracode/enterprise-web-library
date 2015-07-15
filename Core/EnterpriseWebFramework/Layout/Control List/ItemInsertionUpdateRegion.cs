using System;
using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public class ItemInsertionUpdateRegion {
		internal readonly UpdateRegionSet Set;
		internal readonly Func<IEnumerable<string>> NewItemIdGetter;

		/// <summary>
		/// Creates an item-insertion update region.
		/// </summary>
		/// <param name="set"></param>
		/// <param name="newItemIdGetter">A method that executes after the data modification and returns the IDs of the new item(s).</param>
		public ItemInsertionUpdateRegion( UpdateRegionSet set, Func<IEnumerable<string>> newItemIdGetter ) {
			Set = set;
			NewItemIdGetter = newItemIdGetter;
		}
	}
}