namespace EnterpriseWebLibrary.DataAccess.RevisionHistory {
	/// <summary>
	/// A value and the previous revision's value.
	/// </summary>
	public class ValueDelta<T> {
		public readonly string ValueName;
		public readonly T New;
		public readonly T Old;

		internal ValueDelta( string valueName, T @new, T old ) {
			ValueName = valueName;
			New = @new;
			Old = old;
		}

		public bool ValueChanged => !EwlStatics.AreEqual( New, Old );
	}
}