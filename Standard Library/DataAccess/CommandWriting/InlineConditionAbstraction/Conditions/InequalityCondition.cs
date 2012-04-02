using System.Data;
using RedStapler.StandardLibrary.DatabaseSpecification;

namespace RedStapler.StandardLibrary.DataAccess.CommandWriting.InlineConditionAbstraction.Conditions {
	/// <summary>
	/// ISU and Standard Library use only.
	/// </summary>
	public class InequalityCondition: InlineDbCommandCondition {
		/// <summary>
		/// The operator to compare with. For equals, use EqualityCondition instead.
		/// This enum is accessible to developers of systems.
		/// </summary>
		public enum Operator {
			/// <summary>
			/// &gt;
			/// </summary>
			GreaterThan,

			/// <summary>
			/// &gt;=
			/// </summary>
			GreaterThanOrEqualTo,

			/// <summary>
			/// &lt;
			/// </summary>
			LessThan,

			/// <summary>
			/// &lt;=
			/// </summary>
			LessThanOrEqualTo
		}

		private readonly Operator op;
		private readonly InlineDbCommandColumnValue columnValue;

		/// <summary>
		/// ISU use only. Expression will read "valueInDatabase op columnValue".
		/// So new InequalityCondition( Operator.GreaterThan, columnValue ) will turn into "columnName > @columnValue".
		/// </summary>
		public InequalityCondition( Operator op, InlineDbCommandColumnValue columnValue ) {
			this.op = op;
			this.columnValue = columnValue;
		}

		void InlineDbCommandCondition.AddToCommand( IDbCommand command, DatabaseInfo databaseInfo, string parameterName ) {
			columnValue.Parameter.Name = parameterName;
			var operatorString = "<=";
			if( op == Operator.GreaterThan )
				operatorString = ">";
			if( op == Operator.GreaterThanOrEqualTo )
				operatorString = ">=";
			if( op == Operator.LessThan )
				operatorString = "<";

			command.CommandText += columnValue.ColumnName + " " + operatorString + " " + columnValue.Parameter.GetNameForCommandText( databaseInfo );
			command.Parameters.Add( columnValue.Parameter.GetAdoDotNetParameter( databaseInfo ) );
		}
	}
}