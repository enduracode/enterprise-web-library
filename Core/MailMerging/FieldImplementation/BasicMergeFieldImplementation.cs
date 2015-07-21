namespace EnterpriseWebLibrary.MailMerging.FieldImplementation {
	/// <summary>
	/// An implementation for a basic merge field.
	/// </summary>
	public interface BasicMergeFieldImplementation<in RowType, out ValueType> where RowType: class {
		/// <summary>
		/// Gets a description of the field.
		/// </summary>
		string GetDescription();

		/// <summary>
		/// Evaluates the field based on the specified row. The row will never be null.
		/// </summary>
		ValueType Evaluate( RowType row );
	}
}