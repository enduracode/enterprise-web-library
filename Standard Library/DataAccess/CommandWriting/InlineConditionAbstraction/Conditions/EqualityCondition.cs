using System;
using System.Collections.Generic;
using System.Data;
using RedStapler.StandardLibrary.DatabaseSpecification;

namespace RedStapler.StandardLibrary.DataAccess.CommandWriting.InlineConditionAbstraction.Conditions {
	/// <summary>
	/// Standard Library use only.
	/// </summary>
	public class EqualityCondition: InlineDbCommandCondition {
		private readonly InlineDbCommandColumnValue columnValue;

		// IMPORTANT: If we implement Not Equals in this class, then it is extremely important that we modify the generated code to not use the value of the Not Equals condition
		// to initialize mod object data.

		/// <summary>
		/// Standard Library use only.
		/// </summary>
		public EqualityCondition( InlineDbCommandColumnValue columnValue ) {
			this.columnValue = columnValue;
		}

		void InlineDbCommandCondition.AddToCommand( IDbCommand command, DatabaseInfo databaseInfo, string parameterName ) {
			columnValue.Parameter.Name = parameterName;

			if( columnValue.Parameter.ValueIsNull )
				command.CommandText += columnValue.ColumnName + " IS NULL";
			else {
				command.CommandText += columnValue.ColumnName + " = " + columnValue.Parameter.GetNameForCommandText( databaseInfo );
				command.Parameters.Add( columnValue.Parameter.GetAdoDotNetParameter( databaseInfo ) );
			}
		}

		public override bool Equals( object obj ) {
			return Equals( obj as InlineDbCommandCondition );
		}

		public bool Equals( InlineDbCommandCondition other ) {
			var otherEqualityCondition = other as EqualityCondition;
			return otherEqualityCondition != null && StandardLibraryMethods.AreEqual( columnValue, otherEqualityCondition.columnValue );
		}

		public override int GetHashCode() {
			return columnValue.GetHashCode();
		}

		int IComparable.CompareTo( object obj ) {
			var otherCondition = obj as InlineDbCommandCondition;
			if( otherCondition == null && obj != null )
				throw new ArgumentException();
			return CompareTo( otherCondition );
		}

		public int CompareTo( InlineDbCommandCondition other ) {
			if( other == null )
				return 1;
			var otherEqualityCondition = other as EqualityCondition;
			if( otherEqualityCondition == null )
				return DataAccessMethods.CompareCommandConditionTypes( this, other );

			return Comparer<InlineDbCommandColumnValue>.Default.Compare( columnValue, otherEqualityCondition.columnValue );
		}
	}
}