namespace RedStapler.StandardLibrary.MailMerging.FieldImplementation {
	/// <summary>
	/// An implementation for a merge field adapter.
	/// </summary>
	public interface MergeFieldAdapterImplementation<in RowType, out AdaptedFieldRowType> {
		/// <summary>
		/// Gets the prefix that will be added to the name of the adapted merge field.
		/// </summary>
		string NamePrefix { get; }

		/// <summary>
		/// Gets the prefix that will be added to the Microsoft Word name of the adapted merge field.
		/// </summary>
		string MsWordNamePrefix { get; }

		/// <summary>
		/// Gets the suffix that will be added to the description of the adapted merge field.
		/// </summary>
		string DescriptionSuffix { get; }

		/// <summary>
		/// Gets the adapted field row that corresponds to the specified row.
		/// </summary>
		AdaptedFieldRowType GetAdaptedFieldRow( RowType row );
	}
}