using System;
using System.Data;
using RedStapler.StandardLibrary.DatabaseSpecification;

namespace RedStapler.StandardLibrary.DataAccess.CommandWriting.InlineConditionAbstraction.Conditions {
    /// <summary>
    /// Standard Library use only.
    /// </summary>
    public class InCondition: InlineDbCommandCondition {
        private readonly string columnName;
        private readonly string subQuery;

        /// <summary>
        /// Standard Library use only. Nothing in the sub-query is escaped, so do not base any part of it on user input.
        /// </summary>
        public InCondition( string columnName, string subQuery ) {
            this.columnName = columnName;
            this.subQuery = subQuery;
        }

        void InlineDbCommandCondition.AddToCommand( IDbCommand command, DatabaseInfo databaseInfo, string parameterName ) {
            command.CommandText += columnName + " IN ( " + subQuery + " )";
        }

        public override bool Equals( object obj ) {
            return Equals( obj as InlineDbCommandCondition );
        }

        public bool Equals( InlineDbCommandCondition other ) {
            var otherInCondition = other as InCondition;
            return otherInCondition != null && columnName == otherInCondition.columnName && subQuery == otherInCondition.subQuery;
        }

        public override int GetHashCode() {
            return new { columnName, subQuery }.GetHashCode();
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
            var otherInCondition = other as InCondition;
            if( otherInCondition == null )
                return DataAccessMethods.CompareCommandConditionTypes( this, other );

            var columnNameResult = StringComparer.InvariantCulture.Compare( columnName, otherInCondition.columnName );
            return columnNameResult != 0 ? columnNameResult : StringComparer.InvariantCulture.Compare( subQuery, otherInCondition.subQuery );
        }
    }
}