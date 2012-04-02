using RedStapler.StandardLibrary.DataAccess;

namespace RedStapler.StandardLibrary.MailMerging.FieldImplementation {
	/// <summary>
	/// An implementation for a basic merge field.
	/// </summary>
	public interface BasicMergeFieldImplementation<RowType, ValueType> where RowType: class {
		/// <summary>
		/// Gets a description of the field.
		/// </summary>
		string GetDescription( DBConnection cn );

		/// <summary>
		/// Evaluates the field based on the specified row. The row will never be null.
		/// </summary>
		ValueType Evaluate( DBConnection cn, RowType row );
	}
}