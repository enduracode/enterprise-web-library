using System;
using EnterpriseWebLibrary.MailMerging.FieldImplementation;
using EnterpriseWebLibrary.MailMerging.RowTree;

namespace EnterpriseWebLibrary.MailMerging.Fields {
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

		string MergeField<RowType>.GetDescription() {
			return adaptedField.GetDescription() + implementation.DescriptionSuffix;
		}

		MergeValue MergeField<RowType>.CreateValue( string name, string msWordName, Func<string> descriptionGetter, Func<RowType> rowGetter ) {
			return adaptedField.CreateValue( name,
			                                 msWordName,
			                                 descriptionGetter,
			                                 () => {
				                                 var row = rowGetter();
				                                 return row != null ? implementation.GetAdaptedFieldRow( row ) : null;
			                                 } );
		}
	}
}