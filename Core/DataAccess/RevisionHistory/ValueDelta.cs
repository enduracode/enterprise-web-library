using System;

namespace EnterpriseWebLibrary.DataAccess.RevisionHistory {
	/// <summary>
	/// A value and the previous revision's value.
	/// </summary>
	public class ValueDelta<T> {
		public readonly string ValueName;
		public readonly T New;
		private readonly Tuple<T> old;

		internal ValueDelta( string valueName, T @new, Tuple<T> old ) {
			ValueName = valueName;
			New = @new;
			this.old = old;
		}

		/// <summary>
		/// Gets whether there is a previous revision.
		/// </summary>
		public bool HasOld => old != null;

		public T Old => old.Item1;

		public bool ValueChanged => !EwlStatics.AreEqual( New, Old );
	}
}