using System;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.MailMerging.FieldImplementation;

namespace RedStapler.StandardLibrary.MailMerging.RowTree {
	/// <summary>
	/// A combination of a merge field and a data row.
	/// </summary>
	public interface MergeValue {
		/// <summary>
		/// Gets the name of the merge value.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets the name of the merge value for Microsoft Word.
		/// </summary>
		string MsWordName { get; }

		/// <summary>
		/// Gets a description of the value.
		/// </summary>
		string GetDescription( DBConnection cn );
	}

	/// <summary>
	/// A combination of a merge field and a data row.
	/// </summary>
	public interface MergeValue<out ValType>: MergeValue {
		/// <summary>
		/// Evaluates the merge field for the row. Throws an exception if true is specified for ensureValueExists and the merge field has no value for the current
		/// row.
		/// </summary>
		ValType Evaluate( DBConnection cn, bool ensureValueExists );
	}

	internal class MergeValue<RowType, ValType>: MergeValue<ValType> where RowType: class {
		private readonly BasicMergeFieldImplementation<RowType, ValType> field;
		private readonly string name;
		private readonly string msWordName;
		private readonly Func<DBConnection, string> descriptionGetter;
		private readonly Func<DBConnection, RowType> rowGetter;

		internal MergeValue( BasicMergeFieldImplementation<RowType, ValType> field, string name, string msWordName, Func<DBConnection, string> descriptionGetter,
		                     Func<DBConnection, RowType> rowGetter ) {
			this.field = field;
			this.name = name;
			this.msWordName = msWordName;
			this.descriptionGetter = descriptionGetter;
			this.rowGetter = rowGetter;
		}

		public string Name { get { return name; } }
		public string MsWordName { get { return msWordName; } }

		public string GetDescription( DBConnection cn ) {
			return descriptionGetter( cn );
		}

		ValType MergeValue<ValType>.Evaluate( DBConnection cn, bool ensureValueExists ) {
			var row = rowGetter( cn );

			if( field is BasicMergeFieldImplementation<RowType, string> ) {
				var stringValue = row != null ? ( field as BasicMergeFieldImplementation<RowType, string> ).Evaluate( cn, row ) : "";
				if( stringValue == null )
					throw new ApplicationException( "String merge fields must never evaluate to null. (" + Name + ")" );
				if( ensureValueExists && stringValue.Length == 0 )
					throw new MailMergingException( "Merge field " + Name + " has no value for the current row." );
				return (ValType)(object)stringValue;
			}

			var value = row != null ? field.Evaluate( cn, row ) : default( ValType );
			if( ensureValueExists && Equals( value, default( ValType ) ) )
				throw new MailMergingException( "Merge field " + Name + " has no value for the current row." );
			return value;
		}
	}
}