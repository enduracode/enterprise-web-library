#nullable disable
namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// The configuration for a checkbox.
/// </summary>
public class CheckboxSetup {
	/// <summary>
	/// Creates a setup object for a standard checkbox.
	/// </summary>
	/// <param name="displaySetup"></param>
	/// <param name="classes">The classes on the control.</param>
	/// <param name="action">The action that will occur when the user hits Enter on the control. Pass null to use the current default action.</param>
	/// <param name="valueChangedAction">The action that will occur when the value is changed. Pass null for no action.</param>
	/// <param name="pageModificationValue"></param>
	public static CheckboxSetup Create(
		DisplaySetup displaySetup = null, ElementClassSet classes = null, SpecifiedValue<FormAction> action = null, FormAction valueChangedAction = null,
		PageModificationValue<bool> pageModificationValue = null ) =>
		new( displaySetup, false, classes, action, valueChangedAction, pageModificationValue );

	/// <summary>
	/// Creates a setup object for a read-only checkbox.
	/// </summary>
	/// <param name="displaySetup"></param>
	/// <param name="classes">The classes on the control.</param>
	public static CheckboxSetup CreateReadOnly( DisplaySetup displaySetup = null, ElementClassSet classes = null ) =>
		new( displaySetup, true, classes, null, null, null );

	internal readonly DisplaySetup DisplaySetup;
	internal readonly bool IsReadOnly;
	internal readonly ElementClassSet Classes;
	internal readonly FormAction Action;
	internal readonly FormAction ValueChangedAction;
	internal readonly PageModificationValue<bool> PageModificationValue;

	private CheckboxSetup(
		DisplaySetup displaySetup, bool isReadOnly, ElementClassSet classes, SpecifiedValue<FormAction> action, FormAction valueChangedAction,
		PageModificationValue<bool> pageModificationValue ) {
		DisplaySetup = displaySetup;
		IsReadOnly = isReadOnly;
		Classes = classes;
		Action = action != null ? action.Value : FormState.Current.FormControlDefaultAction;
		ValueChangedAction = valueChangedAction;
		PageModificationValue = pageModificationValue;
	}

	internal CheckboxSetup AddPmv() =>
		new(
			DisplaySetup,
			IsReadOnly,
			Classes,
			new SpecifiedValue<FormAction>( Action ),
			ValueChangedAction,
			PageModificationValue ?? new PageModificationValue<bool>() );
}