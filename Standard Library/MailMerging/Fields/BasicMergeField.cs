using System;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.MailMerging.FieldImplementation;
using RedStapler.StandardLibrary.MailMerging.RowTree;

namespace RedStapler.StandardLibrary.MailMerging.Fields {
	/// <summary>
	/// A basic merge field.
	/// </summary>
	internal class BasicMergeField<RowType, ValueType>: MergeField<RowType> where RowType: class {
		private readonly BasicMergeFieldImplementation<RowType, ValueType> implementation;

		internal BasicMergeField( BasicMergeFieldImplementation<RowType, ValueType> implementation ) {
			this.implementation = implementation;
		}

		MergeValue MergeField<RowType>.CreateValue( string namePrefix, string msWordNamePrefix, string descriptionSuffix, Func<DBConnection, RowType> rowGetter ) {
			return new MergeValue<RowType, ValueType>( implementation, namePrefix, msWordNamePrefix, descriptionSuffix, rowGetter );
		}
	}
}