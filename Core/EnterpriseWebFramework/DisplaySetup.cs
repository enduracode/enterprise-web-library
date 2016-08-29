using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The display configuration for a component.
	/// </summary>
	public class DisplaySetup {
		private readonly Tuple<Action<string>, Action<string>> jsShowAndHideStatementAdders;
		private readonly Func<bool> componentsDisplayedPredicate;

		/// <summary>
		/// Creates a display setup object for static display.
		/// </summary>
		/// <param name="componentsDisplayed">Pass true to display the components.</param>
		public DisplaySetup( bool componentsDisplayed ) {
			componentsDisplayedPredicate = () => componentsDisplayed;
		}

		internal DisplaySetup( Tuple<Action<string>, Action<string>> jsShowAndHideStatementAdders, Func<bool> componentsDisplayedPredicate ) {
			this.jsShowAndHideStatementAdders = jsShowAndHideStatementAdders;
			this.componentsDisplayedPredicate = componentsDisplayedPredicate;
		}

		/// <summary>
		/// Gets whether this display setup uses the JavaScript statements that are added.
		/// </summary>
		public bool UsesJsStatements { get { return jsShowAndHideStatementAdders != null; } }

		/// <summary>
		/// Adds the JavaScript statements that show the components.
		/// </summary>
		public void AddJsShowStatements( string statements ) {
			if( jsShowAndHideStatementAdders != null )
				jsShowAndHideStatementAdders.Item1( statements );
		}

		/// <summary>
		/// Adds the JavaScript statements that hide the components.
		/// </summary>
		public void AddJsHideStatements( string statements ) {
			if( jsShowAndHideStatementAdders != null )
				jsShowAndHideStatementAdders.Item2( statements );
		}

		/// <summary>
		/// Gets whether components are displayed. Not available until after the page tree has been built.
		/// </summary>
		public bool ComponentsDisplayed {
			get {
				EwfPage.AssertPageTreeBuilt();
				return componentsDisplayedPredicate();
			}
		}
	}

	public static class DisplaySetupExtensionCreators {
		/// <summary>
		/// Creates a display setup object for display that depends on this page-modification value.
		/// </summary>
		public static DisplaySetup ToDisplaySetup( this PageModificationValue<bool> pageModificationValue, bool componentsDisplayedWhenValueSet = true ) {
			return
				new DisplaySetup(
					Tuple.Create<Action<string>, Action<string>>(
						statements =>
						pageModificationValue.AddJsModificationStatement(
							valueExpression =>
							"if( {0} )".FormatWith( componentsDisplayedWhenValueSet ? valueExpression : "!( {0} )".FormatWith( valueExpression ) ) + " { " + statements + " }" ),
						statements =>
						pageModificationValue.AddJsModificationStatement(
							valueExpression =>
							"if( {0} )".FormatWith( componentsDisplayedWhenValueSet ? "!( {0} )".FormatWith( valueExpression ) : valueExpression ) + " { " + statements + " }" ) ),
					() => pageModificationValue.Value ^ !componentsDisplayedWhenValueSet );
		}

		/// <summary>
		/// Creates a display setup object for display that depends on this page-modification value.
		/// </summary>
		public static DisplaySetup ToDisplaySetup<T>(
			this PageModificationValue<T> pageModificationValue, IEnumerable<T> values, bool componentsDisplayedOnMatch = true ) {
			values = values.ToImmutableArray();
			return
				new DisplaySetup(
					Tuple.Create<Action<string>, Action<string>>(
						statements =>
						pageModificationValue.AddJsModificationStatement(
							valueExpression =>
							"if( [ {0} ].indexOf( {1} ) {2} -1 )".FormatWith(
								StringTools.ConcatenateWithDelimiter( ", ", values.Select( i => "'" + i.ObjectToString( true ) + "'" ).ToArray() ),
								valueExpression,
								componentsDisplayedOnMatch ? "!=" : "==" ) + " { " + statements + " }" ),
						statements =>
						pageModificationValue.AddJsModificationStatement(
							valueExpression =>
							"if( [ {0} ].indexOf( {1} ) {2} -1 )".FormatWith(
								StringTools.ConcatenateWithDelimiter( ", ", values.Select( i => "'" + i.ObjectToString( true ) + "'" ).ToArray() ),
								valueExpression,
								componentsDisplayedOnMatch ? "==" : "!=" ) + " { " + statements + " }" ) ),
					() => values.Contains( pageModificationValue.Value ) ^ !componentsDisplayedOnMatch );
		}
	}
}