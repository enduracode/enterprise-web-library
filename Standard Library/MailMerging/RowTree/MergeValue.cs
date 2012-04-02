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
	public interface MergeValue<ValueType>: MergeValue {
		/// <summary>
		/// Evaluates the merge field for the row. Throws an exception if true is specified for ensureValueExists and the merge field has no value for the current
		/// row.
		/// </summary>
		ValueType Evaluate( DBConnection cn, bool ensureValueExists );
	}

	internal class MergeValue<RowType, ValueType>: MergeValue<ValueType> where RowType: class {
		private readonly BasicMergeFieldImplementation<RowType, ValueType> field;
		private readonly string namePrefix;
		private readonly string msWordNamePrefix;
		private readonly string descriptionSuffix;
		private readonly Func<DBConnection, RowType> rowGetter;

		internal MergeValue( BasicMergeFieldImplementation<RowType, ValueType> field, string namePrefix, string msWordNamePrefix, string descriptionSuffix,
		                     Func<DBConnection, RowType> rowGetter ) {
			this.field = field;
			this.namePrefix = namePrefix;
			this.msWordNamePrefix = msWordNamePrefix;
			this.descriptionSuffix = descriptionSuffix;
			this.rowGetter = rowGetter;
		}

		public string Name {
			get {
				var fieldWithCustomName = field as BasicMergeFieldImplementationWithCustomName<RowType, ValueType>;
				return namePrefix + ( fieldWithCustomName != null ? fieldWithCustomName.Name : field.GetType().Name );
			}
		}

		public string MsWordName {
			get {
				var fieldWithMsWordName = field as BasicMergeFieldImplementationWithMsWordName<RowType, ValueType>;
				return msWordNamePrefix + ( fieldWithMsWordName != null ? fieldWithMsWordName.MsWordName : field.GetType().Name );
			}
		}

		public string GetDescription( DBConnection cn ) {
			return field.GetDescription( cn ) + descriptionSuffix;
		}

		ValueType MergeValue<ValueType>.Evaluate( DBConnection cn, bool ensureValueExists ) {
			var row = rowGetter( cn );

			if( field is BasicMergeFieldImplementation<RowType, string> ) {
				var stringValue = row != null ? ( field as BasicMergeFieldImplementation<RowType, string> ).Evaluate( cn, row ) : "";
				if( stringValue == null )
					throw new ApplicationException( "String merge fields must never evaluate to null. (" + Name + ")" );
				if( ensureValueExists && stringValue.Length == 0 )
					throw new MailMergingException( "Merge field " + Name + " has no value for the current row." );
				return (ValueType)(object)stringValue;
			}

			var value = row != null ? field.Evaluate( cn, row ) : default( ValueType );
			if( ensureValueExists && Equals( value, default( ValueType ) ) )
				throw new MailMergingException( "Merge field " + Name + " has no value for the current row." );
			return value;
		}
	}
}