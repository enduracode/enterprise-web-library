using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
		public bool UsesJsStatements => jsShowAndHideStatementAdders != null;

		/// <summary>
		/// Adds the JavaScript statements that show the components.
		/// </summary>
		public void AddJsShowStatements( string statements ) => jsShowAndHideStatementAdders?.Item1( statements );

		/// <summary>
		/// Adds the JavaScript statements that hide the components.
		/// </summary>
		public void AddJsHideStatements( string statements ) => jsShowAndHideStatementAdders?.Item2( statements );

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

			void DisplayLink.AddJavaScript() {}
		}

		/// <summary>
		/// Creates a display setup object for display that depends on this page-modification-value condition.
		/// </summary>
		public static DisplaySetup ToDisplaySetup( this PageModificationValueCondition pageModificationValueCondition ) {
			return new DisplaySetup(
				Tuple.Create<Action<string>, Action<string>>(
					statements => pageModificationValueCondition.AddJsModificationStatement( expression => "if( {0} )".FormatWith( expression ) + " { " + statements + " }" ),
					statements => pageModificationValueCondition.AddJsModificationStatement(
						expression => "if( !( {0} ) )".FormatWith( expression ) + " { " + statements + " }" ) ),
				() => pageModificationValueCondition.IsTrue );
		}

		// Web Forms compatibility. Remove when EnduraCode goal 790 is complete.
		public static void AddDisplayLink( this PageModificationValueCondition pageModificationValueCondition, IEnumerable<WebControl> controls ) {
			controls = controls.ToImmutableArray();

			foreach( var control in controls )
				pageModificationValueCondition.AddJsModificationStatement( expression => "setElementDisplay( '{0}', {1} );".FormatWith( control.ClientID, expression ) );

			EwfPage.Instance.AddDisplayLink( new DisplayLinkAdapter( controls, () => pageModificationValueCondition.IsTrue ) );
		}
	}
}