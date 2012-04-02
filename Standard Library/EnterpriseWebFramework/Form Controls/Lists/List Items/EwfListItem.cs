namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An item for the list form controls.
	/// </summary>
	public class EwfListItem {
		/// <summary>
		/// Creates a list item.
		/// </summary>
		public static EwfListItem<IdType> Create<IdType>( IdType id, string label ) {
			return new EwfListItem<IdType>( id, label );
		}
	}

	/// <summary>
	/// An item for the list form controls.
	/// </summary>
	public class EwfListItem<IdType> {
		private readonly IdType id;
		private readonly string label;

		internal EwfListItem( IdType id, string label ) {
			this.id = id;
			this.label = label;
		}

		internal IdType Id { get { return id; } }
		internal string Label { get { return label; } }
	}
}