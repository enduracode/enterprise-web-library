namespace EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure.ComponentDisplay;

/// <summary>
/// Focus-dependent data for a displayable element.
/// </summary>
public class DisplayableElementFocusDependentData {
	internal readonly Func<DisplaySetup, ElementFocusDependentData> BaseDataGetter;

	/// <summary>
	/// Creates a displayable-element focus-dependent-data object.
	/// </summary>
	public DisplayableElementFocusDependentData(
		IEnumerable<ElementAttribute>? attributes = null, bool includeIdAttribute = false, string jsInitStatements = "" ) {
		BaseDataGetter = displaySetup => new ElementFocusDependentData(
			displaySetup.ComponentsDisplayed ? attributes : addDisplayStyle( attributes ),
			displaySetup.UsesJsStatements || includeIdAttribute,
			jsInitStatements );
	}

	private IEnumerable<ElementAttribute> addDisplayStyle( IEnumerable<ElementAttribute>? attributes ) {
		const string name = "style";
		const string value = "display: none";

		var added = false;
		if( attributes is not null )
			foreach( var attribute in attributes )
				if( attribute.Name.EqualsIgnoreCase( name ) ) {
					yield return new ElementAttribute( name, attribute.Value + "; " + value );
					added = true;
				}
				else
					yield return attribute;

		if( !added )
			yield return new ElementAttribute( name, value );
	}
}