using System;

namespace EnterpriseWebLibrary.DataAccess.StandardModification {
	/// <summary>
	/// A post delete method and the required table retrieval row collection.
	/// </summary>
	public class PostDeleteCall<T> {
		private readonly Action<T> method;
		private readonly T rowCollection;

		/// <summary>
		/// Creates a post delete call with the specified method and row collection. The row collection should be retrieved based on the conditions in preDelete.
		/// </summary>
		public PostDeleteCall( Action<T> method, T rowCollection ) {
			this.method = method;
			this.rowCollection = rowCollection;
		}

		/// <summary>
		/// EWL use only.
		/// </summary>
		public void Execute() {
			method( rowCollection );
		}
	}
}