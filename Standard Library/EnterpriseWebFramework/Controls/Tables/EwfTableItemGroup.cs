using System;
using System.Collections.Generic;
using System.Linq;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// An item group in an EWF table.
	/// </summary>
	public class EwfTableItemGroup {
		internal readonly Lazy<EwfTableItemGroupRemainingData> RemainingData;
		internal readonly List<Func<EwfTableItem>> Items;

		/// <summary>
		/// Creates an item group.
		/// </summary>
		public EwfTableItemGroup( Func<EwfTableItemGroupRemainingData> remainingDataGetter, IEnumerable<Func<EwfTableItem>> items ) {
			RemainingData = new Lazy<EwfTableItemGroupRemainingData>( remainingDataGetter );
			Items = items.ToList();
		}
	}
}