#nullable disable
namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A CSS length value. See https://www.w3.org/TR/css-values-3/#lengths.
	/// </summary>
	public interface CssLength {
		/// <summary>
		/// Gets the length value.
		/// </summary>
		string Value { get; }
	}
}