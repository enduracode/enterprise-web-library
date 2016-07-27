using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public sealed class ElementId {
		private string id = "";

		/// <summary>
		/// Creates an element ID.
		/// </summary>
		public ElementId() {}

		/// <summary>
		/// Adds the element ID. This can only be called once.
		/// </summary>
		public void AddId( string id ) {
			EwfPage.AssertPageTreeNotBuilt();
			if( id.Length > 0 )
				throw new ApplicationException( "The ID was already added." );
			this.id = id;
		}

		/// <summary>
		/// Gets the element ID, or the empty string if no ID exists. Not available until after the page tree has been built.
		/// </summary>
		public string Id {
			get {
				EwfPage.AssertPageTreeBuilt();
				return id;
			}
		}
	}
}