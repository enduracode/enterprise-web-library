using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Web.UI.WebControls;
using EnterpriseWebLibrary.EnterpriseWebFramework.DisplayLinking;
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
		// Web Forms compatibility. Remove when EnduraCode goal 790 is complete.
		private class DisplayLinkAdapter: DisplayLink {
			private readonly IEnumerable<WebControl> controls;
			private readonly Func<bool> controlsDisplayedPredicate;

			internal DisplayLinkAdapter( IEnumerable<WebControl> controls, Func<bool> controlsDisplayedPredicate ) {
				this.controls = controls;
				this.controlsDisplayedPredicate = controlsDisplayedPredicate;
			}

			void DisplayLink.SetInitialDisplay( PostBackValueDictionary formControlValues ) {
				foreach( var i in controls )
					DisplayLinkingOps.SetControlDisplay( i, controlsDisplayedPredicate() );
			}

			void DisplayLink.AddJavaScript() {
				throw new ApplicationException( "not implemented" );
			}
		}

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

		// Web Forms compatibility. Remove when EnduraCode goal 790 is complete.
		public static void AddDisplayLink(
			this PageModificationValue<bool> pageModificationValue, IEnumerable<WebControl> controls, bool controlsDisplayedWhenValueSet = true ) {
			controls = controls.ToImmutableArray();

			foreach( var control in controls ) {
				pageModificationValue.AddJsModificationStatement(
					valueExpression =>
					"setElementDisplay( '{0}', {1} );".FormatWith(
						control.ClientID,
						controlsDisplayedWhenValueSet ? valueExpression : "!( {0} )".FormatWith( valueExpression ) ) );
			}

			EwfPage.Instance.AddDisplayLink( new DisplayLinkAdapter( controls, () => pageModificationValue.Value ^ !controlsDisplayedWhenValueSet ) );
		}

		// Web Forms compatibility. Remove when EnduraCode goal 790 is complete.
		public static void AddDisplayLink<T>(
			this PageModificationValue<T> pageModificationValue, IEnumerable<T> values, IEnumerable<WebControl> controls, bool controlsDisplayedOnMatch = true ) {
			values = values.ToImmutableArray();
			controls = controls.ToImmutableArray();

			foreach( var control in controls ) {
				pageModificationValue.AddJsModificationStatement(
					valueExpression =>
					"setElementDisplay( '{0}', [ {1} ].indexOf( {2} ) {3} -1 );".FormatWith(
						control.ClientID,
						StringTools.ConcatenateWithDelimiter( ", ", values.Select( i => "'" + i.ObjectToString( true ) + "'" ).ToArray() ),
						valueExpression,
						controlsDisplayedOnMatch ? "!=" : "==" ) );
			}

			EwfPage.Instance.AddDisplayLink( new DisplayLinkAdapter( controls, () => values.Contains( pageModificationValue.Value ) ^ !controlsDisplayedOnMatch ) );
		}
	}
}