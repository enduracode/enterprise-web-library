using System;
using System.Collections.Generic;

namespace RedStapler.StandardLibrary.DataAccess.CommandWriting {
	/// <summary>
	/// A value used in a database command parameter.
	/// </summary>
	public class DbParameterValue: IEquatable<DbParameterValue>, IComparable, IComparable<DbParameterValue> {
		private readonly object value;
		private readonly string dbTypeString;

		/// <summary>
		/// Creates a value with an unspecified type. This is not recommended since it forces the database type to be inferred from the .NET type of the value, and
		/// this process is imperfect and has lead to problems in the past with blobs.
		/// </summary>
		public DbParameterValue( object value ) {
			this.value = value;
		}

		/// <summary>
		/// Creates a value with the specified type.
		/// </summary>
		public DbParameterValue( object value, string dbTypeString ) {
			this.value = value;
			this.dbTypeString = dbTypeString;
		}

		internal object Value { get { return value; } }

		internal string DbTypeString { get { return dbTypeString; } }

		public override bool Equals( object obj ) {
			return Equals( obj as DbParameterValue );
		}

		public bool Equals( DbParameterValue other ) {
			return other != null && StandardLibraryMethods.AreEqual( value, other.value ) && dbTypeString == other.dbTypeString;
		}

		public override int GetHashCode() {
			return value != null ? value.GetHashCode() : -1;
		}

		int IComparable.CompareTo( object obj ) {
			var otherCondition = obj as DbParameterValue;
			if( otherCondition == null && obj != null )
				throw new ArgumentException();
			return CompareTo( otherCondition );
		}

		public int CompareTo( DbParameterValue other ) {
			if( other == null )
				return 1;
			var valueResult = Comparer<object>.Default.Compare( value, other.value );
			return valueResult != 0 ? valueResult : StringComparer.InvariantCulture.Compare( dbTypeString, other.dbTypeString );
		}
	}
}