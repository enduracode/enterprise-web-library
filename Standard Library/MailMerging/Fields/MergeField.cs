using System;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.MailMerging.RowTree;

namespace RedStapler.StandardLibrary.MailMerging.Fields {
	/// <summary>
	/// A merge field.
	/// </summary>
	public interface MergeField<RowType> {
		/// <summary>
		/// Creates a value for the combination of this field and the specified row. Mail Merging subsystem use only.
		/// </summary>
		MergeValue CreateValue( string namePrefix, string msWordNamePrefix, string descriptionSuffix, Func<DBConnection, RowType> rowGetter );
	}
}