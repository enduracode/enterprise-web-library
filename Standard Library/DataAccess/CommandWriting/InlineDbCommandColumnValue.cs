using System;
using System.Collections.Generic;

namespace RedStapler.StandardLibrary.DataAccess.CommandWriting {
	/// <summary>
	/// A column name and a value for use by an inline database command.
	/// </summary>
	public class InlineDbCommandColumnValue: IEquatable<InlineDbCommandColumnValue>, IComparable, IComparable<InlineDbCommandColumnValue> {
		private readonly string columnName;
		private readonly DbParameterValue value;
		private readonly DbCommandParameter parameter;

		/// <summary>
		/// Creates an inline database command column value.
		/// </summary>
		public InlineDbCommandColumnValue( string columnName, DbParameterValue value ) {
			this.columnName = columnName;
			this.value = value;
			parameter = new DbCommandParameter( columnName, value );
		}

		internal string ColumnName { get { return columnName; } }

		internal DbCommandParameter Parameter { get { return parameter; } }

		public override bool Equals( object obj ) {
			return Equals( obj as InlineDbCommandColumnValue );
		}

		public bool Equals( InlineDbCommandColumnValue other ) {
			return other != null && columnName == other.columnName && StandardLibraryMethods.AreEqual( value, other.value );
		}

		public override int GetHashCode() {
			return new { columnName, value }.GetHashCode();
		}

		int IComparable.CompareTo( object obj ) {
			var otherCondition = obj as InlineDbCommandColumnValue;
			if( otherCondition == null && obj != null )
				throw new ArgumentException();
			return CompareTo( otherCondition );
		}

		public int CompareTo( InlineDbCommandColumnValue other ) {
			if( other == null )
				return 1;
			var nameResult = StringComparer.InvariantCulture.Compare( columnName, other.columnName );
			return nameResult != 0 ? nameResult : Comparer<DbParameterValue>.Default.Compare( value, other.value );
		}
	}
}