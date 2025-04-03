using JetBrains.Annotations;

namespace EnterpriseWebLibrary.DataValueManagement;

/// <summary>
/// A value that knows whether it corresponds to existing data, whether it has been initialized, and whether it has changed.
/// </summary>
[ PublicAPI ]
public class DataValue<T>: AbstractDataValue<T>, IEquatable<DataValue<T>> {
	private readonly InitializationAwareValue<T> val = new();

	// True when the value hasn’t been initialized means that data exists, but we don’t have a value. This was the only remaining state combination and is also
	// somewhat logical because the value (i.e. lack thereof) is not in agreement with existing data.
	private bool hasChanged;

	private readonly SpecifiedValue<T>? defaultValue;

	/// <summary>
	/// Creates a data value.
	/// <para>Argument style: We recommend not using named arguments when calling this method due to its ubiquity. Additionally, when multiple parameters are
	/// used, having the values together improves readability since they slightly resemble a ternary conditional expression.</para>
	/// </summary>
	/// <param name="dataExists">Pass true if this value corresponds to existing data.</param>
	/// <param name="existingValueGetter">A function that gets the initial value from existing data. Will only be called if <paramref name="dataExists"/> is true.
	/// </param>
	/// <param name="defaultValue">The value that will be returned from <see cref="Value"/> if data does not exist.</param>
	public DataValue( bool dataExists, Func<T>? existingValueGetter = null, SpecifiedValue<T>? defaultValue = null ) {
		if( dataExists ) {
			if( existingValueGetter is null )
				hasChanged = true;
			else
				val.Value = existingValueGetter();
		}
		else
			this.defaultValue = defaultValue;
	}

	/// <summary>
	/// Gets whether data currently exists for this value.
	/// </summary>
	public bool DataExists => val.Initialized || hasChanged;

	/// <summary>
	/// Gets or sets the value. If you try to get the value before it has been initialized, throws an exception, unless the value does not correspond to existing data and a default value exists.
	/// </summary>
	public T Value {
		get => DataExists || defaultValue is null ? val.Value : defaultValue.Value;
		set {
			if( val.Initialized && EwlStatics.AreEqual( val.Value, value ) )
				return;
			val.Value = value;
			hasChanged = true;
		}
	}

	/// <summary>
	/// Gets whether the value has changed.
	/// </summary>
	public bool HasChanged => val.Initialized && hasChanged;

	/// <summary>
	/// Notifies this value that it has been persisted and now corresponds to existing data. Clears <see cref="HasChanged"/>.
	/// </summary>
	public void NotifyPersisted() {
		hasChanged = !val.Initialized;
	}

	/// <summary>
	/// Creates a new value with the data from this value, optionally transformed into a new type.
	/// </summary>
	public DataValue<NewType> CreateNewValue<NewType>( Func<T, NewType> valueSelector ) =>
		new( DataExists, val.Initialized ? () => valueSelector( Value ) : null );

	public override bool Equals( object? obj ) => Equals( obj as DataValue<T> );

	public bool Equals( DataValue<T>? other ) => other != null && EwlStatics.AreEqual( val, other.val );

	public override int GetHashCode() => val.GetHashCode();
}