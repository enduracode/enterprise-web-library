using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The display configuration for a component.
	/// </summary>
	public class DisplaySetup {
		private readonly bool? componentsDisplayed;
		private readonly PageModificationValue<bool> pageModificationValue;
		private readonly bool? componentsDisplayedWhenValueSet;

		/// <summary>
		/// Creates a display setup object for static display.
		/// </summary>
		/// <param name="componentsDisplayed">Pass true to display the components.</param>
		public DisplaySetup( bool componentsDisplayed ) {
			this.componentsDisplayed = componentsDisplayed;
		}

		internal DisplaySetup( PageModificationValue<bool> pageModificationValue, bool? componentsDisplayedWhenValueSet ) {
			this.pageModificationValue = pageModificationValue;
			this.componentsDisplayedWhenValueSet = componentsDisplayedWhenValueSet;
		}

		/// <summary>
		/// Gets whether this display setup uses the JavaScript statements that are added.
		/// </summary>
		public bool UsesJsStatements { get { return pageModificationValue != null; } }

		/// <summary>
		/// Adds the JavaScript statements that show the components.
		/// </summary>
		public void AddJsShowStatements( string statements ) {
			if( pageModificationValue != null )
				pageModificationValue.AddJsModificationStatement(
					valueExpression =>
					"if( {0} )".FormatWith( componentsDisplayedWhenValueSet.Value ? valueExpression : "!( {0} )".FormatWith( valueExpression ) ) + " { " + statements + " }" );
		}

		/// <summary>
		/// Adds the JavaScript statements that hide the components.
		/// </summary>
		public void AddJsHideStatements( string statements ) {
			if( pageModificationValue != null )
				pageModificationValue.AddJsModificationStatement(
					valueExpression =>
					"if( {0} )".FormatWith( componentsDisplayedWhenValueSet.Value ? "!( {0} )".FormatWith( valueExpression ) : valueExpression ) + " { " + statements + " }" );
		}

		/// <summary>
		/// Gets whether components are displayed. Not available until after the page tree has been built.
		/// </summary>
		public bool ComponentsDisplayed {
			get {
				EwfPage.AssertPageTreeBuilt();
				return componentsDisplayed ?? pageModificationValue.Value ^ !componentsDisplayedWhenValueSet.Value;
			}
		}
	}

	public static class DisplaySetupExtensionCreators {
		/// <summary>
		/// Creates a display setup object for display that depends on this page-modification value.
		/// </summary>
		public static DisplaySetup ToDisplaySetup( this PageModificationValue<bool> pageModificationValue, bool componentsDisplayedWhenValueSet = true ) {
			return new DisplaySetup( pageModificationValue, componentsDisplayedWhenValueSet );
		}
	}
}