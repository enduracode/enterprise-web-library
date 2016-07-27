using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public sealed class ElementIdSet {
		private readonly List<string> ids = new List<string>();

		/// <summary>
		/// Creates an element-ID set.
		/// </summary>
		public ElementIdSet() {}

		/// <summary>
		/// Adds an element ID to this set.
		/// </summary>
		public void AddId( string id ) {
			EwfPage.AssertPageTreeNotBuilt();
			ids.Add( id );
		}

		/// <summary>
		/// Gets the element IDs in this set, which are not available until after the page tree has been built.
		/// </summary>
		public IReadOnlyCollection<string> Ids {
			get {
				EwfPage.AssertPageTreeBuilt();
				return ids;
			}
		}
	}
}