namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// The display configuration for a component.
/// </summary>
public class DisplaySetup {
	private readonly Tuple<Action<string>, Action<string>>? jsShowAndHideStatementAdders;
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
	/// Gets whether components are displayed. Not available until after rendering preparation has started.
	/// </summary>
	public bool ComponentsDisplayed {
		get {
			PageBase.AssertRenderingPreparationStarted();
			return componentsDisplayedPredicate();
		}
	}
}

public static class DisplaySetupExtensionCreators {
	/// <summary>
	/// Creates a display setup object for display that depends on this page-modification-value condition.
	/// </summary>
	public static DisplaySetup ToDisplaySetup( this PageModificationValueCondition pageModificationValueCondition ) =>
		new(
			Tuple.Create<Action<string>, Action<string>>(
				statements => pageModificationValueCondition.AddJsModificationStatement(
					expression => "if( {0} )".FormatWith( expression ) + " { " + statements + " }" ),
				statements => pageModificationValueCondition.AddJsModificationStatement(
					expression => "if( !( {0} ) )".FormatWith( expression ) + " { " + statements + " }" ) ),
			() => pageModificationValueCondition.IsTrue );
}