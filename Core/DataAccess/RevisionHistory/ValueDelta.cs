using JetBrains.Annotations;

namespace EnterpriseWebLibrary.DataAccess.RevisionHistory;

/// <summary>
/// A value and the previous revision's value.
/// </summary>
[ PublicAPI ]
public class ValueDelta<T> {
	public readonly string ValueName;
	public readonly T New;
	private readonly SpecifiedValue<T>? old;

	internal ValueDelta( string valueName, T @new, SpecifiedValue<T>? old ) {
		ValueName = valueName;
		New = @new;
		this.old = old;
	}

	/// <summary>
	/// Gets whether there is a previous revision.
	/// </summary>
	public bool HasOld => old != null;

	public T Old => old!.Value;

	public bool ValueChanged => !EwlStatics.AreEqual( New, Old );
}