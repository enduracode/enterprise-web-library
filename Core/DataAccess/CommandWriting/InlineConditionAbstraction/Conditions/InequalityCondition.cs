﻿using System.Data;
using EnterpriseWebLibrary.DatabaseSpecification;

namespace EnterpriseWebLibrary.DataAccess.CommandWriting.InlineConditionAbstraction.Conditions;

/// <summary>
/// EWL Core and Development Utility use only.
/// </summary>
public class InequalityCondition: InlineDbCommandCondition {
	/// <summary>
	/// The operator to compare with. For equals, use EqualityCondition instead.
	/// This enum is accessible to developers of systems.
	/// </summary>
	public enum Operator {
		/// <summary>
		/// !=
		/// </summary>
		NotEqualTo,

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
		var parameter = columnValue.GetParameter( name: parameterName );
		string operatorString;
		switch( op ) {
			case Operator.NotEqualTo:
				operatorString = "!=";
				break;
			case Operator.LessThan:
				operatorString = "<";
				break;
			case Operator.GreaterThan:
				operatorString = ">";
				break;
			case Operator.LessThanOrEqualTo:
				operatorString = "<=";
				break;
			case Operator.GreaterThanOrEqualTo:
				operatorString = ">=";
				break;
			default:
				throw new ApplicationException( "unknown operator" );
		}

		command.CommandText += columnValue.GetColumnIdentifier( databaseInfo ) + " " + operatorString + " " + parameter.GetNameForCommandText( databaseInfo );
		command.Parameters.Add( parameter.GetAdoDotNetParameter( databaseInfo ) );
	}

	public override bool Equals( object? obj ) => Equals( obj as InlineDbCommandCondition );

	public bool Equals( InlineDbCommandCondition? other ) {
		var otherInequalityCondition = other as InequalityCondition;
		return otherInequalityCondition != null && op == otherInequalityCondition.op && EwlStatics.AreEqual( columnValue, otherInequalityCondition.columnValue );
	}

	public override int GetHashCode() => new { op, columnValue }.GetHashCode();

	int IComparable.CompareTo( object? obj ) {
		var otherCondition = obj as InlineDbCommandCondition;
		if( otherCondition == null && obj != null )
			throw new ArgumentException();
		return CompareTo( otherCondition );
	}

	public int CompareTo( InlineDbCommandCondition? other ) {
		if( other == null )
			return 1;
		var otherInequalityCondition = other as InequalityCondition;
		if( otherInequalityCondition == null )
			return DataAccessMethods.CompareCommandConditionTypes( this, other );

		var opResult = EwlStatics.Compare( op, otherInequalityCondition.op );
		return opResult != 0 ? opResult : EwlStatics.Compare( columnValue, otherInequalityCondition.columnValue );
	}
}