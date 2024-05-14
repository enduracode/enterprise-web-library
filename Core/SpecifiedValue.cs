namespace EnterpriseWebLibrary;

/// <summary>
/// A value that has been specified.
/// </summary>
public class SpecifiedValue<T> {
	public T? Value { get; }

	/// <summary>
	/// Creates a specified value.
	/// </summary>
	public SpecifiedValue( T? value ) {
		Value = value;
	}
}