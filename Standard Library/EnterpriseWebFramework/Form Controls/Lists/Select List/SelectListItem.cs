namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	internal class SelectListItem<ItemIdType> {
		private readonly EwfListItem<ItemIdType> item;
		private readonly bool isValid;
		private readonly bool isPlaceholder;

		internal SelectListItem( EwfListItem<ItemIdType> item, bool isValid, bool isPlaceholder ) {
			this.item = item;
			this.isValid = isValid;
			this.isPlaceholder = isPlaceholder;
		}

		internal EwfListItem<ItemIdType> Item { get { return item; } }
		internal string StringId { get { return item.Id.ObjectToString( true ); } }
		internal bool IsValid { get { return isValid; } }
		internal bool IsPlaceholder { get { return isPlaceholder; } }
	}
}