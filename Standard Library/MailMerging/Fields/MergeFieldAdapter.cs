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

		string MergeField<RowType>.Name { get { return implementation.NamePrefix + adaptedField.Name; } }

		string MergeField<RowType>.MsWordName { get { return implementation.MsWordNamePrefix + adaptedField.MsWordName; } }

		string MergeField<RowType>.GetDescription( DBConnection cn ) {
			return adaptedField.GetDescription( cn ) + implementation.DescriptionSuffix;
		}

		MergeValue MergeField<RowType>.CreateValue( string name, string msWordName, Func<DBConnection, string> descriptionGetter,
		                                            Func<DBConnection, RowType> rowGetter ) {
			return adaptedField.CreateValue( name,
			                                 msWordName,
			                                 descriptionGetter,
			                                 delegate( DBConnection cn ) {
			                                 	var row = rowGetter( cn );
			                                 	return row != null ? implementation.GetAdaptedFieldRow( cn, row ) : null;
			                                 } );
		}
	}
}