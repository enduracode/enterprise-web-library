using System;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.MailMerging.RowTree;

namespace RedStapler.StandardLibrary.MailMerging.Fields {
	/// <summary>
	/// A merge field.
	/// </summary>
	public interface MergeField<in RowType> {
		/// <summary>
		/// Gets the name of the merge field.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets the name of the merge field for Microsoft Word.
		/// </summary>
		string MsWordName { get; }

		/// <summary>
		/// Gets a description of the field.
		/// </summary>
		string GetDescription( DBConnection cn );

		/// <summary>
		/// Creates a value for the combination of this field and the specified row. Mail Merging subsystem use only.
		/// </summary>
		MergeValue CreateValue( string name, string msWordName, Func<DBConnection, string> descriptionGetter, Func<DBConnection, RowType> rowGetter );
	}
}