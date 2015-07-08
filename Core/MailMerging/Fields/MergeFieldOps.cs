using RedStapler.StandardLibrary.MailMerging.FieldImplementation;

namespace RedStapler.StandardLibrary.MailMerging.Fields {
	/// <summary>
	/// Methods that create merge fields.
	/// </summary>
	// We don't use constructors to create merge fields because they require explicit declaration of type parameters.
	public static class MergeFieldOps {
		/// <summary>
		/// Creates a basic merge field.
		/// </summary>
		public static MergeField<RowType> CreateBasicField<RowType, ValueType>( BasicMergeFieldImplementation<RowType, ValueType> implementation )
			where RowType: class {
			return new BasicMergeField<RowType, ValueType>( implementation );
		}

		/// <summary>
		/// Creates a merge field adapter.
		/// </summary>
		public static MergeField<RowType> CreateFieldAdapter<RowType, AdaptedFieldRowType>( MergeField<AdaptedFieldRowType> adaptedField,
		                                                                                    MergeFieldAdapterImplementation<RowType, AdaptedFieldRowType>
		                                                                                    	implementation ) where RowType: class where AdaptedFieldRowType: class {
			return new MergeFieldAdapter<RowType, AdaptedFieldRowType>( adaptedField, implementation );
		}
	}
}