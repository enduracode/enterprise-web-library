using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A condition that depends on a page-modification value.
	/// </summary>
	public class PageModificationValueCondition {
		private readonly Action<Func<string, string>> jsModificationStatementAdder;
		private readonly Func<bool> isTruePredicate;

		internal PageModificationValueCondition( Action<Func<string, string>> jsModificationStatementAdder, Func<bool> isTruePredicate ) {
			this.jsModificationStatementAdder = jsModificationStatementAdder;
			this.isTruePredicate = isTruePredicate;
		}

		/// <summary>
		/// Adds a JavaScript statement that should be executed when the underlying page-modification value changes.
		/// </summary>
		/// <param name="statementGetter">A function that takes the condition's expression and returns a complete statement. Do not pass null.</param>
		public void AddJsModificationStatement( Func<string, string> statementGetter ) => jsModificationStatementAdder( statementGetter );

		/// <summary>
		/// Gets whether the condition is true. Not available until after the page tree has been built.
		/// </summary>
		public bool IsTrue => isTruePredicate();
	}

	public static class PageModificationValueConditionExtensionCreators {
		/// <summary>
		/// Creates a condition that depends on this page-modification value.
		/// </summary>
		public static PageModificationValueCondition ToCondition( this PageModificationValue<bool> pageModificationValue, bool isTrueWhenValueSet = true ) {
			return new PageModificationValueCondition(
				statementGetter => pageModificationValue.AddJsModificationStatement(
					valueExpression => statementGetter( isTrueWhenValueSet ? valueExpression : "!( {0} )".FormatWith( valueExpression ) ) ),
				() => pageModificationValue.Value ^ !isTrueWhenValueSet );
		}

		/// <summary>
		/// Creates a condition that depends on this page-modification value.
		/// </summary>
		public static PageModificationValueCondition ToCondition<T>(
			this PageModificationValue<T> pageModificationValue, IEnumerable<T> values, bool isTrueOnMatch = true ) {
			values = values.ToImmutableArray();
			return new PageModificationValueCondition(
				statementGetter => pageModificationValue.AddJsModificationStatement(
					valueExpression => "[ {0} ].indexOf( {1} ) {2} -1".FormatWith(
						StringTools.ConcatenateWithDelimiter( ", ", values.Select( i => "'" + i.ObjectToString( true ) + "'" ).ToArray() ),
						valueExpression,
						isTrueOnMatch ? "!=" : "==" ) ),
				() => values.Contains( pageModificationValue.Value ) ^ !isTrueOnMatch );
		}
	}
}