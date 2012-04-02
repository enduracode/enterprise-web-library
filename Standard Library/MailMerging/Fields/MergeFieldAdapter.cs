using System;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.MailMerging.FieldImplementation;
using RedStapler.StandardLibrary.MailMerging.RowTree;

namespace RedStapler.StandardLibrary.MailMerging.Fields {
	/// <summary>
	/// A merge field adapter, which allows a merge field to be used with other fields that have a different row type.
	/// </summary>
	internal class MergeFieldAdapter<RowType, AdaptedFieldRowType>: MergeField<RowType> where RowType: class where AdaptedFieldRowType: class {
		private readonly MergeField<AdaptedFieldRowType> adaptedField;
		private readonly MergeFieldAdapterImplementation<RowType, AdaptedFieldRowType> implementation;

		internal MergeFieldAdapter( MergeField<AdaptedFieldRowType> adaptedField, MergeFieldAdapterImplementation<RowType, AdaptedFieldRowType> implementation ) {
			this.adaptedField = adaptedField;
			this.implementation = implementation;
		}

		MergeValue MergeField<RowType>.CreateValue( string namePrefix, string msWordNamePrefix, string descriptionSuffix, Func<DBConnection, RowType> rowGetter ) {
			return adaptedField.CreateValue( namePrefix + implementation.NamePrefix,
			                                 msWordNamePrefix + implementation.MsWordNamePrefix,
			                                 implementation.DescriptionSuffix + descriptionSuffix,
			                                 delegate( DBConnection cn ) {
			                                 	var row = rowGetter( cn );
			                                 	return row != null ? implementation.GetAdaptedFieldRow( cn, row ) : null;
			                                 } );
		}
	}
}