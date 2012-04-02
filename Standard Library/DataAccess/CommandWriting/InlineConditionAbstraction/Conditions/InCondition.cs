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
	}
}