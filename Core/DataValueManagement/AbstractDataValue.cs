namespace EnterpriseWebLibrary.DataValueManagement;

/// <summary>
/// A value that knows whether it corresponds to existing data and whether it has been initialized.
/// </summary>
public interface AbstractDataValue<T> {
	/// <summary>
	/// Gets whether data currently exists for this value.
	/// </summary>
	bool DataExists { get; }

	/// <summary>
	/// Gets or sets the value. Throws an exception if you try to get the value before it has been initialized.
	/// </summary>
	T Value { get; set; }

	/// <summary>
	/// Creates a new value with the data from this value, optionally transformed into a new type.
	/// </summary>
	DataValue<NewType> CreateNewValue<NewType>( Func<T, NewType> valueSelector ) => new( DataExists, () => valueSelector( Value ) );
}