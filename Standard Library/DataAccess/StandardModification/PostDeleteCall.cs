using System;

namespace RedStapler.StandardLibrary.DataAccess.StandardModification {
	/// <summary>
	/// A post delete method and the required table retrieval row collection.
	/// </summary>
	public class PostDeleteCall<T> {
		private readonly Action<DBConnection, T> method;
		private readonly T rowCollection;

		/// <summary>
		/// Creates a post delete call with the specified method and row collection. The row collection should be retrieved based on the conditions in preDelete.
		/// </summary>
		public PostDeleteCall( Action<DBConnection, T> method, T rowCollection ) {
			this.method = method;
			this.rowCollection = rowCollection;
		}

		/// <summary>
		/// Standard Library use only.
		/// </summary>
		public void Execute( DBConnection cn ) {
			method( cn, rowCollection );
		}
	}
}