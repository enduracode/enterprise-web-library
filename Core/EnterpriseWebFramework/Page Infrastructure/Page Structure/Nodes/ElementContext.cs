namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public sealed class ElementContext {
		/// <summary>
		/// The generated ID for the element.
		/// </summary>
		public readonly string Id;

		internal ElementContext( string id ) {
			Id = id;
		}
	}
}