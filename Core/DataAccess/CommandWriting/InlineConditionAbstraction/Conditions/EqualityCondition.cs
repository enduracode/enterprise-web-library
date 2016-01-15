using System;
using System.Data;
using EnterpriseWebLibrary.DatabaseSpecification;

namespace EnterpriseWebLibrary.DataAccess.CommandWriting.InlineConditionAbstraction.Conditions {
	/// <summary>
	/// EWL use only.
	/// </summary>
	public class EqualityCondition: InlineDbCommandCondition {
		private readonly InlineDbCommandColumnValue columnValue;

		// IMPORTANT: If we implement Not Equals in this class, then it is extremely important that we modify the generated code to not use the value of the Not Equals condition
		// to initialize mod object data.

		/// <summary>
		/// EWL use only.
		/// </summary>
		public EqualityCondition( InlineDbCommandColumnValue columnValue ) {
			this.columnValue = columnValue;
		}

		void InlineDbCommandCondition.AddToCommand( IDbCommand command, DatabaseInfo databaseInfo, string parameterName ) {
			var parameter = columnValue.GetParameter( name: parameterName );

			if( parameter.ValueIsNull )
				command.CommandText += columnValue.ColumnName + " IS NULL";
			else {
				command.CommandText += columnValue.ColumnName + " = " + parameter.GetNameForCommandText( databaseInfo );
				command.Parameters.Add( parameter.GetAdoDotNetParameter( databaseInfo ) );
			}
		}

		public override bool Equals( object obj ) {
			return Equals( obj as InlineDbCommandCondition );
		}

		public bool Equals( InlineDbCommandCondition other ) {
			var otherEqualityCondition = other as EqualityCondition;
			return otherEqualityCondition != null && EwlStatics.AreEqual( columnValue, otherEqualityCondition.columnValue );
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

			return EwlStatics.Compare( columnValue, otherEqualityCondition.columnValue );
		}
	}
}