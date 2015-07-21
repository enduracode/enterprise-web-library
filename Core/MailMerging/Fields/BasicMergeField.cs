using System;
using EnterpriseWebLibrary.MailMerging.FieldImplementation;
using EnterpriseWebLibrary.MailMerging.RowTree;

namespace EnterpriseWebLibrary.MailMerging.Fields {
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

		string MergeField<RowType>.GetDescription() {
			return implementation.GetDescription();
		}

		MergeValue MergeField<RowType>.CreateValue( string name, string msWordName, Func<string> descriptionGetter, Func<RowType> rowGetter ) {
			return new MergeValue<RowType, ValType>( implementation, name, msWordName, descriptionGetter, rowGetter );
		}
	}
}