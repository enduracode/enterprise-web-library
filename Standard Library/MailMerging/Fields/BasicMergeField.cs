using System;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.MailMerging.FieldImplementation;
using RedStapler.StandardLibrary.MailMerging.RowTree;

namespace RedStapler.StandardLibrary.MailMerging.Fields {
	/// <summary>
	/// A basic merge field.
	/// </summary>
	internal class BasicMergeField<RowType, ValType>: MergeField<RowType> where RowType: class {
		private readonly BasicMergeFieldImplementation<RowType, ValType> implementation;

		internal BasicMergeField( BasicMergeFieldImplementation<RowType, ValType> implementation ) {
			this.implementation = implementation;
		}

		string MergeField<RowType>.Name {
			get {
				var impWithCustomName = implementation as BasicMergeFieldImplementationWithCustomName<RowType, ValType>;
				return impWithCustomName != null ? impWithCustomName.Name : implementation.GetType().Name;
			}
		}

		string MergeField<RowType>.MsWordName {
			get {
				var impWithMsWordName = implementation as BasicMergeFieldImplementationWithMsWordName<RowType, ValType>;
				return impWithMsWordName != null ? impWithMsWordName.MsWordName : implementation.GetType().Name;
			}
		}

		string MergeField<RowType>.GetDescription( DBConnection cn ) {
			return implementation.GetDescription( cn );
		}

		MergeValue MergeField<RowType>.CreateValue( string name, string msWordName, Func<DBConnection, string> descriptionGetter,
		                                            Func<DBConnection, RowType> rowGetter ) {
			return new MergeValue<RowType, ValType>( implementation, name, msWordName, descriptionGetter, rowGetter );
		}
	}
}