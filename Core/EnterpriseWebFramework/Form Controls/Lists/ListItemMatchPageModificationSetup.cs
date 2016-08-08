using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public sealed class ListItemMatchPageModificationSetup<ItemIdType> {
		internal readonly PageModificationValue<bool> PageModificationValue;
		internal readonly IEnumerable<ItemIdType> ItemIds;

		internal ListItemMatchPageModificationSetup( PageModificationValue<bool> pageModificationValue, IEnumerable<ItemIdType> itemIds ) {
			PageModificationValue = pageModificationValue;
			ItemIds = itemIds;
		}
	}

	public static class ListItemMatchPageModificationSetupExtensionCreators {
		/// <summary>
		/// Creates a page-modification setup for a list form control that sets this value when one of the specified items is selected.
		/// </summary>
		public static ListItemMatchPageModificationSetup<ItemIdType> ToListItemMatchSetup<ItemIdType>(
			this PageModificationValue<bool> pageModificationValue, IEnumerable<ItemIdType> itemIds ) {
			return new ListItemMatchPageModificationSetup<ItemIdType>( pageModificationValue, itemIds );
		}
	}
}