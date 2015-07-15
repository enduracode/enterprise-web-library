namespace EnterpriseWebLibrary.MailMerging.FieldImplementation {
	/// <summary>
	/// An implementation for a basic merge field with a separate name for Microsoft Word.
	/// </summary>
	public interface BasicMergeFieldImplementationWithMsWordName<in RowType, out ValueType>: BasicMergeFieldImplementation<RowType, ValueType> where RowType: class {
		/// <summary>
		/// Gets the Microsoft Word name of this merge field.
		/// </summary>
		string MsWordName { get; }
	}
}