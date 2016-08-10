namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public sealed class HiddenFieldId {
		private readonly ElementId id;

		/// <summary>
		/// Creates a hidden-field ID.
		/// </summary>
		public HiddenFieldId() {
			id = new ElementId();
		}

		internal void AddId( string id ) {
			this.id.AddId( id );
		}

		/// <summary>
		/// Gets the hidden-field ID, or the empty string if no ID exists. Not available until after the page tree has been built.
		/// </summary>
		public string Id { get { return id.Id; } }
	}
}