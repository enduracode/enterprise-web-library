namespace RedStapler.StandardLibrary.MailMerging.FieldImplementation {
	/// <summary>
	/// An implementation for a basic merge field with a custom name.
	/// </summary>
	public interface BasicMergeFieldImplementationWithCustomName<in RowType, out ValueType>: BasicMergeFieldImplementationWithMsWordName<RowType, ValueType>
		where RowType: class {
		/// <summary>
		/// Gets the name of this merge field.
		/// </summary>
		string Name { get; }
	}
}