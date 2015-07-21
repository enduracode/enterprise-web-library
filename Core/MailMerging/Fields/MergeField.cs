using System;
using EnterpriseWebLibrary.MailMerging.RowTree;

namespace EnterpriseWebLibrary.MailMerging.Fields {
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
		string GetDescription();

		/// <summary>
		/// Creates a value for the combination of this field and the specified row. Mail Merging subsystem use only.
		/// </summary>
		MergeValue CreateValue( string name, string msWordName, Func<string> descriptionGetter, Func<RowType> rowGetter );
	}
}