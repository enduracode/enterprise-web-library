using JetBrains.Annotations;

namespace EnterpriseWebLibrary;

/// <summary>
/// A value that knows whether it has been initialized and whether it has changed.
/// </summary>
[ PublicAPI ]
public class DataValue<T>: IEquatable<DataValue<T>> {
	private readonly InitializationAwareValue<T> val = new();

	public bool Changed { get; private set; }

	public T Value {
		get => val.Value;
		set {
			if( val.Initialized && EwlStatics.AreEqual( val.Value, value ) )
				return;
			val.Value = value;
			Changed = true;
		}
	}

	public void ClearChanged() {
		Changed = false;
	}

	public override bool Equals( object? obj ) => Equals( obj as DataValue<T> );

	public bool Equals( DataValue<T>? other ) => other != null && EwlStatics.AreEqual( val, other.val );

	public override int GetHashCode() => val.GetHashCode();
}